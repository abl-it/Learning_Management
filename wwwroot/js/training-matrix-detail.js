/* =========================================================
   Training Matrix - dokumen per Department (Phase 1B-3)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi: window.tmDetailCfg (Views/TrainingMatrix/Detail.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const cfg = window.tmDetailCfg;
    const urls = cfg.urls;
    const matrixId = cfg.matrixId;
    const LAYERS = [1, 2, 3];

    let data = null;                 // MatrixDetailDto
    let editable = false;
    const selCells = new Set();      // "subDepartmentId:layer"
    const rowSel = new Set();        // matrixDetailId
    let table = null;
    let applyModal = null;

    // cache untuk modal Apply
    let groups = null;
    let courseOptions = null;
    const applyCourses = new Set();

    /* ---------- helpers ---------- */

    function esc(value) {
        return $('<div>').text(value ?? '').html();
    }

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    function toast(icon, text) {
        return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' });
    }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    /** POST form-urlencoded; array dikirim sebagai field berulang. */
    function post(url, obj) {
        const body = new URLSearchParams();
        body.append('__RequestVerificationToken', token());

        Object.keys(obj).forEach(function (k) {
            const v = obj[k];
            if (v === null || v === undefined || v === '') return;
            if (Array.isArray(v)) v.forEach(x => body.append(k, x));
            else body.append(k, v);
        });

        return $.ajax({
            url: url,
            method: 'POST',
            data: body.toString(),
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8'
        });
    }

    function statusClass(status) {
        return {
            'Draft': 'bg-secondary',
            'Pending for HRGA Head': 'bg-warning text-dark',
            'Approved': 'bg-success',
            'Rejected': 'bg-danger'
        }[status] || 'bg-secondary';
    }

    function reqBadge(req) {
        return req === 'Mandatory'
            ? '<span class="badge bg-danger">Mandatory</span>'
            : '<span class="badge bg-info text-dark">Recommended</span>';
    }

    const cellKey = (sub, layer) => `${sub}:${layer}`;

    /* ---------- load ---------- */

    function load() {
        return $.getJSON(urls.getDetail, { id: matrixId })
            .done(function (res) {
                if (!res.success) {
                    toast('error', res.message || 'Gagal memuat data.');
                    return;
                }

                data = res.data;
                editable = data.isEditable === true;

                // bersihkan pilihan yang sudah tidak valid
                const subs = new Set(data.subDepartments.map(s => s.subDepartmentId));
                Array.from(selCells).forEach(k => { if (!subs.has(Number(k.split(':')[0]))) selCells.delete(k); });
                rowSel.clear();

                renderHeader();
                renderGrid();
                refreshTable();
                updateButtons();
            })
            .fail(ajaxError);
    }

    /* ---------- header + workflow ---------- */

    function renderHeader() {
        $('#tmTitle').text(data.departmentAbrv);
        $('#tmStatus').attr('class', `badge ms-2 align-middle ${statusClass(data.status)}`).text(data.status);

        const codes = Array.from(new Set(data.subDepartments.map(s => s.departmentCode))).join(', ');
        $('#tmSub').text(`Department code: ${codes || '-'} · ${data.subDepartments.length} Sub Department · ${data.rows.length} baris`);

        // notice
        const $n = $('#tmNotice').removeClass('alert-warning alert-danger alert-info d-none');
        if (data.status === 'Rejected') {
            $n.addClass('alert-danger').html(`<b>Rejected.</b> ${esc(data.notes || '')} Perbaiki lalu ajukan ulang.`);
        } else if (data.docStatus === 1) {
            $n.addClass('alert-warning').html('Menunggu approval HRGA Head. Dokumen terkunci.');
        } else if (data.docStatus === 2) {
            $n.addClass('alert-info').html('Approved. Dokumen terkunci; perubahan harus melalui Change Request.');
        } else {
            $n.addClass('d-none');
        }

        $('#tmGrid').toggleClass('tm-locked', !editable);

        // tombol workflow dari server (sesuai role + status)
        const $a = $('#tmActions').empty();
        const label = {
            'Request for Approve': ['btn-primary', 'fa-paper-plane', 'Submit for Approval'],
            'Approve': ['btn-success', 'fa-check', 'Approve'],
            'Reject': ['btn-danger', 'fa-xmark', 'Reject']
        };

        (data.actions || []).forEach(function (act) {
            const l = label[act];
            if (!l) return;
            $a.append(`<button type="button" class="btn btn-sm ${l[0]} tm-action" data-action="${esc(act)}"><i class="fas ${l[1]} me-1"></i>${l[2]}</button>`);
        });

        // Change Request untuk matrix Approved
        if (data.docStatus === 2) {
            if (data.activeChangeRequestId) {
                $a.append(`<a class="btn btn-sm btn-outline-primary" href="${urls.crDetail}?id=${data.activeChangeRequestId}"><i class="fas fa-file-pen me-1"></i>Open Change Request</a>`);
            } else if (data.canRequestChange) {
                $a.append('<button type="button" class="btn btn-sm btn-warning" id="tmBtnChange"><i class="fas fa-file-pen me-1"></i>Request Change</button>');
            }
        }

        if (editable) {
            $a.append('<button type="button" class="btn btn-sm btn-outline-danger" id="tmBtnDelete"><i class="fas fa-trash me-1"></i>Delete</button>');
        }
    }

    /** Hitung sel Sub Dept x Layer yang belum punya course (untuk peringatan saat submit). */
    function emptyCellSummary() {
        const filled = new Set(data.rows.map(r => cellKey(r.subDepartmentId, r.layer)));
        const perLayer = { 1: 0, 2: 0, 3: 0 };
        let empty = 0;

        data.subDepartments.forEach(function (s) {
            LAYERS.forEach(function (l) {
                if (!filled.has(cellKey(s.subDepartmentId, l))) {
                    perLayer[l]++;
                    empty++;
                }
            });
        });

        return {
            empty: empty,
            total: data.subDepartments.length * LAYERS.length,
            text: `Sel kosong ${empty}/${data.subDepartments.length * LAYERS.length}: L1 ${perLayer[1]}, L2 ${perLayer[2]}, L3 ${perLayer[3]}`
        };
    }

    function runAction(action) {
        const isReject = action === 'Reject';
        const isSubmit = action === 'Request for Approve';
        const gap = isSubmit ? emptyCellSummary() : null;
        const titles = {
            'Request for Approve': 'Ajukan approval?',
            'Approve': 'Approve Training Matrix?',
            'Reject': 'Reject Training Matrix?'
        };

        Swal.fire({
            icon: isReject ? 'warning' : 'question',
            title: titles[action] || action,
            html: (gap && gap.empty > 0)
                ? `<div class="alert alert-warning text-start small mb-0">Masih ada <b>${gap.empty}</b> dari ${gap.total} sel (Sub Dept × Layer) yang kosong.<br>${esc(gap.text)}.<br>Informasi ini ikut dicatat pada pengajuan. Lanjutkan?</div>`
                : undefined,
            input: 'textarea',
            inputLabel: isReject ? 'Alasan (wajib)' : 'Catatan (opsional)',
            inputAttributes: { maxlength: 250 },
            showCancelButton: true,
            confirmButtonText: 'Ya',
            cancelButtonText: 'Batal',
            inputValidator: v => (isReject && !(v || '').trim()) ? 'Alasan wajib diisi.' : undefined
        }).then(function (r) {
            if (!r.isConfirmed) return;

            let remarks = (r.value || '').trim();

            if (gap && gap.empty > 0) {
                const tag = `[${gap.text}]`;
                // batas remarks di server 250 karakter: catatan user dipotong, tag selalu utuh
                remarks = `${remarks.slice(0, Math.max(0, 250 - tag.length - 1))} ${tag}`.trim();
            }

            post(urls.transition, { id: matrixId, workflowAction: action, remarks: remarks })
                .done(function (res) {
                    if (!res.success) {
                        toast('error', res.message);
                        return;
                    }
                    toast('success', `Status: ${res.data.status}`).then(load);
                })
                .fail(ajaxError);
        });
    }

    function requestChange() {
        Swal.fire({
            icon: 'question',
            title: 'Buat Change Request?',
            text: 'Perubahan atas matrix yang sudah Approved harus lewat Change Request dan approval HRGA Head.',
            input: 'textarea',
            inputLabel: 'Alasan perubahan (wajib)',
            inputAttributes: { maxlength: 500 },
            showCancelButton: true,
            confirmButtonText: 'Buat',
            cancelButtonText: 'Batal',
            inputValidator: v => !(v || '').trim() ? 'Alasan wajib diisi.' : undefined
        }).then(function (r) {
            if (!r.isConfirmed) return;

            post(urls.crCreate, { matrixId: matrixId, reason: (r.value || '').trim() })
                .done(function (res) {
                    if (!res.success) {
                        toast('error', res.message);
                        return;
                    }
                    window.location.href = `${urls.crDetail}?id=${res.data.changeRequestId}`;
                })
                .fail(ajaxError);
        });
    }

    function deleteMatrix() {
        Swal.fire({
            icon: 'warning',
            title: 'Hapus Training Matrix?',
            text: 'Dokumen akan dihapus.',
            showCancelButton: true,
            confirmButtonText: 'Hapus',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            post(urls.remove, { id: matrixId })
                .done(function (res) {
                    if (!res.success) {
                        toast('error', res.message);
                        return;
                    }
                    window.location.href = urls.index;
                })
                .fail(ajaxError);
        });
    }

    /* ---------- grid ---------- */

    function renderGrid() {
        const counts = {};
        data.rows.forEach(function (r) {
            const k = cellKey(r.subDepartmentId, r.layer);
            counts[k] = counts[k] || { m: 0, r: 0 };
            if (r.requirement === 'Mandatory') counts[k].m++; else counts[k].r++;
        });

        const multiDept = new Set(data.subDepartments.map(s => s.departmentCode)).size > 1;
        const $body = $('#tmGrid tbody').empty();

        if (data.subDepartments.length === 0) {
            $body.append('<tr><td colspan="4" class="text-center text-muted p-3">Tidak ada Sub Department aktif.</td></tr>');
            return;
        }

        data.subDepartments.forEach(function (s) {
            let tr = `<tr><th class="tm-rowhdr fw-normal" data-sub="${s.subDepartmentId}">`
                + `${esc(s.subDepartmentCode)} - ${esc(s.subDepartmentName)}`
                + (multiDept ? ` <span class="badge bg-light text-dark border ms-1">${esc(s.departmentCode)}</span>` : '')
                + '</th>';

            LAYERS.forEach(function (l) {
                const k = cellKey(s.subDepartmentId, l);
                const c = counts[k];
                const sel = selCells.has(k) ? ' tm-selected' : '';
                const inner = c
                    ? `${c.m ? `<span class="badge bg-danger me-1">M ${c.m}</span>` : ''}${c.r ? `<span class="badge bg-info text-dark">R ${c.r}</span>` : ''}`
                    : '<span class="text-muted">–</span>';
                tr += `<td class="tm-cell text-center${sel}" data-key="${k}">${inner}</td>`;
            });

            $body.append(tr + '</tr>');
        });

        $('#tmSelInfo').text(selCells.size
            ? `${selCells.size} sel terpilih`
            : 'Klik sel / nama baris / header layer untuk memilih.');
    }

    function toggleKeys(keys) {
        const allSelected = keys.every(k => selCells.has(k));
        keys.forEach(k => allSelected ? selCells.delete(k) : selCells.add(k));
        rowSel.clear();
        renderGrid();
        refreshTable();
        updateButtons();
    }

    /* ---------- detail table ---------- */

    function filteredRows() {
        if (selCells.size === 0) return data.rows;
        return data.rows.filter(r => selCells.has(cellKey(r.subDepartmentId, r.layer)));
    }

    function initTable() {
        const subName = {};

        table = $('#tmRows').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 50,
            order: [[1, 'asc'], [2, 'asc'], [3, 'asc']],
            columns: [
                {
                    title: '<input type="checkbox" class="form-check-input" id="tmChkAll" />',
                    data: null, orderable: false, className: 'text-center', width: '30px',
                    render: r => `<input type="checkbox" class="form-check-input tm-rowchk" data-id="${r.matrixDetailId}" ${rowSel.has(r.matrixDetailId) ? 'checked' : ''} />`
                },
                {
                    title: 'Sub Department', data: 'subDepartmentId',
                    render: function (id) {
                        const s = data?.subDepartments.find(x => x.subDepartmentId === id);
                        return s ? `${esc(s.subDepartmentCode)} - ${esc(s.subDepartmentName)}` : id;
                    }
                },
                { title: 'Layer', data: 'layer', className: 'text-center' },
                { title: 'Course', data: null, render: r => `<span class="fw-medium">${esc(r.courseCode)}</span> - ${esc(r.courseName)}` },
                { title: 'Hours', data: 'durationHours', className: 'text-center' },
                { title: 'Requirement', data: 'requirement', className: 'text-center', render: d => reqBadge(d) },
                {
                    title: 'Source', data: 'sourceCourseGroupName',
                    render: (d, t, r) => r.sourceCourseGroupId
                        ? esc(d || `Group #${r.sourceCourseGroupId} (deleted)`)
                        : '<span class="text-muted">manual</span>'
                }
            ]
        });
    }

    function refreshTable() {
        const rows = filteredRows();
        table.column(0).visible(editable);
        table.clear().rows.add(rows).draw();

        $('#tmRowsInfo').text(selCells.size
            ? `(${rows.length} baris dari sel terpilih)`
            : `(${rows.length} baris, semua sel)`);
    }

    function updateButtons() {
        $('.tm-edit-only').toggleClass('d-none', !editable);
        $('#tmBtnApply').prop('disabled', !(editable && selCells.size > 0));
        $('#tmBtnMandatory, #tmBtnRecommended, #tmBtnRemove').prop('disabled', !(editable && rowSel.size > 0));
    }

    /* ---------- row actions ---------- */

    function rowIds() { return Array.from(rowSel); }

    function setRequirement(req) {
        post(urls.setRequirement, { id: matrixId, detailIds: rowIds(), requirement: req })
            .done(res => res.success ? load() : toast('error', res.message))
            .fail(ajaxError);
    }

    function removeRows() {
        Swal.fire({
            icon: 'warning',
            title: `Hapus ${rowSel.size} baris?`,
            showCancelButton: true,
            confirmButtonText: 'Hapus',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            post(urls.removeRows, { id: matrixId, detailIds: rowIds() })
                .done(res => res.success ? load() : toast('error', res.message))
                .fail(ajaxError);
        });
    }

    /* ---------- modal Apply ---------- */

    function renderApplyList() {
        const q = ($('#amSearch').val() || '').trim().toLowerCase();
        const $box = $('#amCourseList').empty();

        const rows = (courseOptions || []).filter(c =>
            !q || c.courseCode.toLowerCase().includes(q) || c.courseName.toLowerCase().includes(q));

        if (rows.length === 0) {
            $box.append('<div class="p-3 text-muted text-center small">Tidak ada course.</div>');
        }

        rows.forEach(function (c) {
            const id = `amc_${c.courseId}`;
            $box.append(
                `<label class="d-flex align-items-start gap-2 px-3 py-2 border-bottom mb-0" for="${id}" style="cursor:pointer">
                    <input class="form-check-input mt-1 am-chk" type="checkbox" id="${id}" data-id="${c.courseId}" ${applyCourses.has(c.courseId) ? 'checked' : ''} />
                    <span class="flex-grow-1"><span class="fw-medium">${esc(c.courseCode)}</span> - ${esc(c.courseName)}</span>
                    <span class="text-muted small text-nowrap">${esc(c.durationHours)} h</span>
                </label>`);
        });

        $('#amCount').text(applyCourses.size);
    }

    function openApply() {
        applyCourses.clear();
        $('#amSearch').val('');
        $('#amReqM').prop('checked', true);
        $('#amSummary').text(`${selCells.size} sel (Sub Department × Layer) terpilih.`);

        const tasks = [];

        if (groups === null) {
            tasks.push($.getJSON(urls.groups).then(res => { groups = res.success ? res.data.filter(g => g.isActive && g.courseCount > 0) : []; }));
        }
        if (courseOptions === null) {
            tasks.push($.getJSON(urls.courseOptions).then(res => { courseOptions = res.success ? res.data : []; }));
        }

        $.when.apply($, tasks).done(function () {
            const $g = $('#amGroup').empty().append('<option value="">— tidak pakai group —</option>');
            groups.forEach(g => $g.append(`<option value="${g.courseGroupId}">${esc(g.groupName)} (${g.courseCount} course)</option>`));

            renderApplyList();
            applyModal.show();
        }).fail(ajaxError);
    }

    function submitApply() {
        const groupId = $('#amGroup').val();

        if (!groupId && applyCourses.size === 0) {
            toast('warning', 'Pilih Course Group atau minimal satu course.');
            return;
        }

        const $btn = $('#amApply').prop('disabled', true);

        post(urls.apply, {
            MatrixId: matrixId,
            CourseGroupId: groupId,
            CourseIds: Array.from(applyCourses),
            Requirement: $('input[name="amReq"]:checked').val(),
            Targets: Array.from(selCells)
        }).done(function (res) {
            if (!res.success) {
                toast('error', res.message);
                return;
            }
            applyModal.hide();
            toast('success', `Ditambahkan ${res.data.added} baris, dilewati ${res.data.skipped} (sudah ada).`).then(load);
        }).fail(ajaxError)
          .always(() => $btn.prop('disabled', false));
    }

    /* ---------- init ---------- */

    $(function () {
        applyModal = new bootstrap.Modal(document.getElementById('tmApplyModal'));

        initTable();

        // grid selection (semua pengguna boleh memilih untuk memfilter; Apply hanya bila editable)
        $('#tmGrid')
            .on('click', '.tm-cell', function () { toggleKeys([String($(this).data('key'))]); })
            .on('click', '.tm-rowhdr', function () {
                const sub = $(this).data('sub');
                toggleKeys(LAYERS.map(l => cellKey(sub, l)));
            })
            .on('click', '.tm-colhdr', function () {
                const layer = $(this).data('layer');
                toggleKeys(data.subDepartments.map(s => cellKey(s.subDepartmentId, layer)));
            });

        $('#tmClearSel').on('click', function () {
            selCells.clear();
            rowSel.clear();
            renderGrid();
            refreshTable();
            updateButtons();
        });

        // detail rows
        $('#tmRows')
            .on('change', '.tm-rowchk', function () {
                const id = Number($(this).data('id'));
                if (this.checked) rowSel.add(id); else rowSel.delete(id);
                updateButtons();
            });

        $(document).on('change', '#tmChkAll', function () {
            const on = this.checked;
            table.rows({ search: 'applied' }).data().toArray().forEach(r => on ? rowSel.add(r.matrixDetailId) : rowSel.delete(r.matrixDetailId));
            $('#tmRows .tm-rowchk').prop('checked', on);
            updateButtons();
        });

        $('#tmBtnMandatory').on('click', () => setRequirement('Mandatory'));
        $('#tmBtnRecommended').on('click', () => setRequirement('Recommended'));
        $('#tmBtnRemove').on('click', removeRows);

        // header actions
        $('#tmActions')
            .on('click', '.tm-action', function () { runAction($(this).data('action')); })
            .on('click', '#tmBtnDelete', deleteMatrix)
            .on('click', '#tmBtnChange', requestChange);

        // apply modal
        $('#tmBtnApply').on('click', openApply);
        $('#amApply').on('click', submitApply);
        $('#amSearch').on('input', renderApplyList);
        $('#amCourseList').on('change', '.am-chk', function () {
            const id = Number($(this).data('id'));
            if (this.checked) applyCourses.add(id); else applyCourses.delete(id);
            $('#amCount').text(applyCourses.size);
        });

        load();
    });

})();
