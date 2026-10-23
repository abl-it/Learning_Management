/* =========================================================
   Change Request atas Training Matrix (Phase 1B-4)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi: window.crCfg (Views/MatrixChangeRequest/Detail.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const cfg = window.crCfg;
    const urls = cfg.urls;
    const crId = cfg.changeRequestId;
    const LAYERS = [1, 2, 3];

    let data = null;                 // ChangeRequestDetailDto
    let editable = false;
    const selCells = new Set();      // "subDepartmentId:layer"
    const matrixSel = new Set();     // matrixDetailId (untuk Mark for removal)
    const changeSel = new Set();     // changeRequestDetailId (untuk Undo)
    let rowsTable = null;
    let changesTable = null;
    let addModal = null;

    let groups = null;
    let courseOptions = null;
    const addCourses = new Set();

    /* ---------- helpers ---------- */

    function esc(value) { return $('<div>').text(value ?? '').html(); }

    function token() { return $('input[name="__RequestVerificationToken"]').first().val(); }

    function toast(icon, text) { return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' }); }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    function post(url, obj) {
        const body = new URLSearchParams();
        body.append('__RequestVerificationToken', token());

        Object.keys(obj).forEach(function (k) {
            const v = obj[k];
            if (v === null || v === undefined || v === '') return;
            if (Array.isArray(v)) v.forEach(x => body.append(k, x)); else body.append(k, v);
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
        if (!req) return '';
        return req === 'Mandatory'
            ? '<span class="badge bg-danger">Mandatory</span>'
            : '<span class="badge bg-info text-dark">Recommended</span>';
    }

    const cellKey = (sub, layer) => `${sub}:${layer}`;

    function subLabel(id) {
        const s = data?.subDepartments.find(x => x.subDepartmentId === id);
        return s ? `${esc(s.subDepartmentCode)} - ${esc(s.subDepartmentName)}` : id;
    }

    // course yang sudah ditandai Remove: "sub:layer:course"
    function removedSet() {
        return new Set(data.changes.filter(c => c.actionType === 'Remove')
            .map(c => `${c.subDepartmentId}:${c.layer}:${c.courseId}`));
    }

    /* ---------- load ---------- */

    function load() {
        return $.getJSON(urls.getDetail, { id: crId })
            .done(function (res) {
                if (!res.success) {
                    toast('error', res.message || 'Gagal memuat data.');
                    return;
                }

                data = res.data;
                editable = data.isEditable === true;
                matrixSel.clear();
                changeSel.clear();

                renderHeader();
                renderGrid();
                refreshTables();
                updateButtons();
            })
            .fail(ajaxError);
    }

    /* ---------- header + workflow ---------- */

    function renderHeader() {
        $('#crTitle').text(`${data.departmentAbrv} #${data.changeRequestId}`);
        $('#crStatus').attr('class', `badge ms-2 align-middle ${statusClass(data.status)}`).text(data.status);
        $('#crSub').text(`${data.changes.length} perubahan · dibuat oleh ${data.createdBy || '-'}`);
        $('#crReason').text(data.reason || '-');
        $('#crBack').attr('href', `${urls.matrixDetail}?id=${data.matrixId}`);

        const $n = $('#crNotice').removeClass('alert-warning alert-danger alert-success d-none');
        if (data.status === 'Rejected') {
            $n.addClass('alert-danger').html(`<b>Rejected.</b> ${esc(data.notes || '')} Perbaiki lalu ajukan ulang.`);
        } else if (data.docStatus === 1) {
            $n.addClass('alert-warning').html('Menunggu approval HRGA Head. Change Request terkunci.');
        } else if (data.docStatus === 2) {
            $n.addClass('alert-success').html('Approved. Perubahan sudah diterapkan ke matrix.');
        } else {
            $n.addClass('d-none');
        }

        $('#crGrid').toggleClass('tm-locked', !editable);

        const $a = $('#crActions').empty();
        const label = {
            'Request for Approve': ['btn-primary', 'fa-paper-plane', 'Submit for Approval'],
            'Approve': ['btn-success', 'fa-check', 'Approve'],
            'Reject': ['btn-danger', 'fa-xmark', 'Reject']
        };

        (data.actions || []).forEach(function (act) {
            const l = label[act];
            if (!l) return;
            $a.append(`<button type="button" class="btn btn-sm ${l[0]} cr-action" data-action="${esc(act)}"><i class="fas ${l[1]} me-1"></i>${l[2]}</button>`);
        });

        if (editable) {
            $a.append('<button type="button" class="btn btn-sm btn-outline-danger" id="crBtnDelete"><i class="fas fa-trash me-1"></i>Delete</button>');
        }
    }

    function runAction(action) {
        const isReject = action === 'Reject';
        const titles = {
            'Request for Approve': 'Ajukan approval Change Request?',
            'Approve': 'Approve Change Request? Perubahan langsung diterapkan ke matrix.',
            'Reject': 'Reject Change Request?'
        };

        Swal.fire({
            icon: isReject ? 'warning' : 'question',
            title: titles[action] || action,
            input: 'textarea',
            inputLabel: isReject ? 'Alasan (wajib)' : 'Catatan (opsional)',
            inputAttributes: { maxlength: 250 },
            showCancelButton: true,
            confirmButtonText: 'Ya',
            cancelButtonText: 'Batal',
            inputValidator: v => (isReject && !(v || '').trim()) ? 'Alasan wajib diisi.' : undefined
        }).then(function (r) {
            if (!r.isConfirmed) return;

            post(urls.transition, { id: crId, workflowAction: action, remarks: (r.value || '').trim() })
                .done(function (res) {
                    if (!res.success) { toast('error', res.message); return; }
                    toast('success', `Status: ${res.data.status}`).then(load);
                })
                .fail(ajaxError);
        });
    }

    function deleteCr() {
        Swal.fire({
            icon: 'warning',
            title: 'Hapus Change Request?',
            showCancelButton: true,
            confirmButtonText: 'Hapus',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            post(urls.remove, { id: crId })
                .done(function (res) {
                    if (!res.success) { toast('error', res.message); return; }
                    window.location.href = `${urls.matrixDetail}?id=${data.matrixId}`;
                })
                .fail(ajaxError);
        });
    }

    /* ---------- grid ---------- */

    function renderGrid() {
        const counts = {};
        const bump = (k, f) => { counts[k] = counts[k] || { m: 0, r: 0, add: 0, rem: 0 }; counts[k][f]++; };

        data.matrixRows.forEach(r => bump(cellKey(r.subDepartmentId, r.layer), r.requirement === 'Mandatory' ? 'm' : 'r'));
        data.changes.forEach(c => bump(cellKey(c.subDepartmentId, c.layer), c.actionType === 'Add' ? 'add' : 'rem'));

        const multiDept = new Set(data.subDepartments.map(s => s.departmentCode)).size > 1;
        const $body = $('#crGrid tbody').empty();

        data.subDepartments.forEach(function (s) {
            let tr = `<tr><th class="cr-rowhdr fw-normal" data-sub="${s.subDepartmentId}">`
                + `${esc(s.subDepartmentCode)} - ${esc(s.subDepartmentName)}`
                + (multiDept ? ` <span class="badge bg-light text-dark border ms-1">${esc(s.departmentCode)}</span>` : '')
                + '</th>';

            LAYERS.forEach(function (l) {
                const k = cellKey(s.subDepartmentId, l);
                const c = counts[k];
                const sel = selCells.has(k) ? ' cr-selected' : '';
                let inner = '<span class="text-muted">–</span>';

                if (c) {
                    inner = `${c.m ? `<span class="badge bg-danger me-1">M ${c.m}</span>` : ''}`
                        + `${c.r ? `<span class="badge bg-info text-dark me-1">R ${c.r}</span>` : ''}`
                        + `${c.add ? `<span class="badge bg-success me-1">+${c.add}</span>` : ''}`
                        + `${c.rem ? `<span class="badge bg-dark">−${c.rem}</span>` : ''}`;
                }

                tr += `<td class="cr-cell text-center${sel}" data-key="${k}">${inner}</td>`;
            });

            $body.append(tr + '</tr>');
        });

        $('#crSelInfo').text(selCells.size
            ? `${selCells.size} sel terpilih`
            : 'Klik sel / nama baris / header layer untuk memilih.');
    }

    function toggleKeys(keys) {
        const all = keys.every(k => selCells.has(k));
        keys.forEach(k => all ? selCells.delete(k) : selCells.add(k));
        matrixSel.clear();
        renderGrid();
        refreshTables();
        updateButtons();
    }

    /* ---------- tables ---------- */

    function initTables() {
        rowsTable = $('#crRows').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 25,
            order: [[1, 'asc'], [2, 'asc'], [3, 'asc']],
            createdRow: function (row, r) {
                if (removedSet().has(`${r.subDepartmentId}:${r.layer}:${r.courseId}`)) {
                    $(row).addClass('cr-removed');
                }
            },
            columns: [
                {
                    title: '', data: null, orderable: false, className: 'text-center', width: '30px',
                    render: function (r) {
                        const marked = removedSet().has(`${r.subDepartmentId}:${r.layer}:${r.courseId}`);
                        return marked
                            ? '<span class="badge bg-dark">Remove</span>'
                            : `<input type="checkbox" class="form-check-input cr-matchk" data-id="${r.matrixDetailId}" ${matrixSel.has(r.matrixDetailId) ? 'checked' : ''} />`;
                    }
                },
                { title: 'Sub Department', data: 'subDepartmentId', render: id => subLabel(id) },
                { title: 'Layer', data: 'layer', className: 'text-center' },
                { title: 'Course', data: null, render: r => `<span class="fw-medium">${esc(r.courseCode)}</span> - ${esc(r.courseName)}` },
                { title: 'Requirement', data: 'requirement', className: 'text-center', render: d => reqBadge(d) }
            ]
        });

        changesTable = $('#crChanges').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 25,
            order: [[1, 'asc'], [2, 'asc'], [3, 'asc'], [4, 'asc']],
            columns: [
                {
                    title: '', data: null, orderable: false, className: 'text-center', width: '30px',
                    render: r => `<input type="checkbox" class="form-check-input cr-chgchk" data-id="${r.changeRequestDetailId}" ${changeSel.has(r.changeRequestDetailId) ? 'checked' : ''} />`
                },
                {
                    title: 'Action', data: 'actionType', className: 'text-center',
                    render: d => d === 'Add' ? '<span class="badge bg-success">Add</span>' : '<span class="badge bg-dark">Remove</span>'
                },
                { title: 'Sub Department', data: 'subDepartmentId', render: id => subLabel(id) },
                { title: 'Layer', data: 'layer', className: 'text-center' },
                { title: 'Course', data: null, render: r => `<span class="fw-medium">${esc(r.courseCode)}</span> - ${esc(r.courseName)}` },
                { title: 'Requirement', data: 'requirement', className: 'text-center', render: d => reqBadge(d) },
                {
                    title: 'Source', data: 'sourceCourseGroupName',
                    render: (d, t, r) => r.actionType !== 'Add' ? ''
                        : r.sourceCourseGroupId ? esc(d || `Group #${r.sourceCourseGroupId} (deleted)`)
                        : '<span class="text-muted">manual</span>'
                }
            ]
        });
    }

    function refreshTables() {
        const rows = selCells.size === 0
            ? data.matrixRows
            : data.matrixRows.filter(r => selCells.has(cellKey(r.subDepartmentId, r.layer)));

        rowsTable.column(0).visible(editable);
        rowsTable.clear().rows.add(rows).draw();
        $('#crRowsInfo').text(selCells.size ? `(${rows.length} baris dari sel terpilih)` : `(${rows.length} baris, semua sel)`);

        changesTable.column(0).visible(editable);
        changesTable.clear().rows.add(data.changes).draw();
        $('#crChangesInfo').text(`(${data.changes.length})`);
    }

    function updateButtons() {
        $('.cr-edit-only').toggleClass('d-none', !editable);
        $('#crBtnAdd').prop('disabled', !(editable && selCells.size > 0));
        $('#crBtnRemove').prop('disabled', !(editable && matrixSel.size > 0));
        $('#crBtnUndo').prop('disabled', !(editable && changeSel.size > 0));
    }

    /* ---------- actions ---------- */

    function markRemove() {
        post(urls.markRemove, { id: crId, matrixDetailIds: Array.from(matrixSel) })
            .done(res => res.success ? load() : toast('error', res.message))
            .fail(ajaxError);
    }

    function undoChanges() {
        post(urls.undoChanges, { id: crId, changeDetailIds: Array.from(changeSel) })
            .done(res => res.success ? load() : toast('error', res.message))
            .fail(ajaxError);
    }

    /* ---------- modal Add ---------- */

    function renderAddList() {
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
                    <input class="form-check-input mt-1 am-chk" type="checkbox" id="${id}" data-id="${c.courseId}" ${addCourses.has(c.courseId) ? 'checked' : ''} />
                    <span class="flex-grow-1"><span class="fw-medium">${esc(c.courseCode)}</span> - ${esc(c.courseName)}</span>
                    <span class="text-muted small text-nowrap">${esc(c.durationHours)} h</span>
                </label>`);
        });

        $('#amCount').text(addCourses.size);
    }

    function openAdd() {
        addCourses.clear();
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

            renderAddList();
            addModal.show();
        }).fail(ajaxError);
    }

    function submitAdd() {
        const groupId = $('#amGroup').val();

        if (!groupId && addCourses.size === 0) {
            toast('warning', 'Pilih Course Group atau minimal satu course.');
            return;
        }

        const $btn = $('#amApply').prop('disabled', true);

        post(urls.addCourses, {
            ChangeRequestId: crId,
            CourseGroupId: groupId,
            CourseIds: Array.from(addCourses),
            Requirement: $('input[name="amReq"]:checked').val(),
            Targets: Array.from(selCells)
        }).done(function (res) {
            if (!res.success) { toast('error', res.message); return; }
            addModal.hide();
            toast('success', `Ditambahkan ${res.data.added} perubahan, dilewati ${res.data.skipped} (sudah ada di matrix / request).`).then(load);
        }).fail(ajaxError)
          .always(() => $btn.prop('disabled', false));
    }

    /* ---------- init ---------- */

    $(function () {
        addModal = new bootstrap.Modal(document.getElementById('crAddModal'));

        initTables();

        $('#crGrid')
            .on('click', '.cr-cell', function () { toggleKeys([String($(this).data('key'))]); })
            .on('click', '.cr-rowhdr', function () {
                const sub = $(this).data('sub');
                toggleKeys(LAYERS.map(l => cellKey(sub, l)));
            })
            .on('click', '.cr-colhdr', function () {
                const layer = $(this).data('layer');
                toggleKeys(data.subDepartments.map(s => cellKey(s.subDepartmentId, layer)));
            });

        $('#crClearSel').on('click', function () {
            selCells.clear();
            matrixSel.clear();
            renderGrid();
            refreshTables();
            updateButtons();
        });

        $('#crRows').on('change', '.cr-matchk', function () {
            const id = Number($(this).data('id'));
            if (this.checked) matrixSel.add(id); else matrixSel.delete(id);
            updateButtons();
        });

        $('#crChanges').on('change', '.cr-chgchk', function () {
            const id = Number($(this).data('id'));
            if (this.checked) changeSel.add(id); else changeSel.delete(id);
            updateButtons();
        });

        $('#crBtnRemove').on('click', markRemove);
        $('#crBtnUndo').on('click', undoChanges);
        $('#crBtnAdd').on('click', openAdd);

        $('#crActions')
            .on('click', '.cr-action', function () { runAction($(this).data('action')); })
            .on('click', '#crBtnDelete', deleteCr);

        $('#amApply').on('click', submitAdd);
        $('#amSearch').on('input', renderAddList);
        $('#amCourseList').on('change', '.am-chk', function () {
            const id = Number($(this).data('id'));
            if (this.checked) addCourses.add(id); else addCourses.delete(id);
            $('#amCount').text(addCourses.size);
        });

        load();
    });

})();
