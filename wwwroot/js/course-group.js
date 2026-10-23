/* =========================================================
   Course Group (Training Matrix - Phase 1A)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi dari Views/CourseGroup/Index.cshtml (window.courseGroupCfg)
   ========================================================= */

(function () {
    'use strict';

    const cfg = window.courseGroupCfg;
    const urls = cfg.urls;
    const canEdit = cfg.canEdit === true;

    let table = null;
    let modal = null;
    let readOnly = false;

    // Daftar course pada picker: { courseId, courseCode, courseName, durationHours, isActive }
    let courses = [];
    // CourseId yang dicentang
    const selected = new Set();

    /* ---------- helpers ---------- */

    function esc(value) {
        return $('<div>').text(value ?? '').html();
    }

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    function fmtDate(value) {
        if (!value) return '';
        const d = new Date(value);
        if (isNaN(d)) return '';
        const p = n => String(n).padStart(2, '0');
        return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()} ${p(d.getHours())}:${p(d.getMinutes())}`;
    }

    function toast(icon, text) {
        Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' });
    }

    function ajaxError(xhr) {
        const msg = xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.';
        toast('error', msg);
    }

    /* ---------- list ---------- */

    function loadTable() {
        $.getJSON(urls.getList)
            .done(function (res) {
                if (!res.success) {
                    toast('error', res.message || 'Gagal memuat data.');
                    return;
                }
                table.clear().rows.add(res.data).draw();
            })
            .fail(ajaxError);
    }

    function initTable() {
        table = $('#cgTable').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 25,
            order: [[0, 'asc']],
            columns: [
                { title: 'Group Name', data: 'groupName', render: d => `<span class="fw-medium">${esc(d)}</span>` },
                { title: 'Description', data: 'description', render: d => esc(d) },
                { title: 'Courses', data: 'courseCount', className: 'text-center' },
                {
                    title: 'Status', data: 'isActive', className: 'text-center',
                    render: d => d
                        ? '<span class="badge bg-success">Active</span>'
                        : '<span class="badge bg-secondary">Inactive</span>'
                },
                {
                    title: 'Last Update', data: null,
                    render: r => r.modifiedDate
                        ? `${fmtDate(r.modifiedDate)}<div class="text-muted small">${esc(r.modifiedBy)}</div>`
                        : `${fmtDate(r.createdDate)}<div class="text-muted small">${esc(r.createdBy)}</div>`
                },
                {
                    title: 'Action', data: null, orderable: false, className: 'text-center text-nowrap',
                    render: function (r) {
                        let html = `<button type="button" class="btn btn-sm btn-outline-secondary me-1 cg-view" data-id="${r.courseGroupId}" title="View"><i class="fas fa-eye"></i></button>`;
                        if (canEdit) {
                            html += `<button type="button" class="btn btn-sm btn-outline-primary me-1 cg-edit" data-id="${r.courseGroupId}" title="Edit"><i class="fas fa-pen"></i></button>`;
                            html += `<button type="button" class="btn btn-sm btn-outline-danger cg-del" data-id="${r.courseGroupId}" data-name="${esc(r.groupName)}" title="Delete"><i class="fas fa-trash"></i></button>`;
                        }
                        return html;
                    }
                }
            ]
        });
    }

    /* ---------- course picker ---------- */

    function renderCourseList() {
        const q = ($('#cgSearch').val() || '').trim().toLowerCase();
        const onlySel = $('#cgOnlySelected').is(':checked');
        const $box = $('#cgCourseList').empty();

        const rows = courses.filter(c => {
            if (onlySel && !selected.has(c.courseId)) return false;
            if (!q) return true;
            return c.courseCode.toLowerCase().includes(q) || c.courseName.toLowerCase().includes(q);
        });

        if (rows.length === 0) {
            $box.append('<div class="p-3 text-muted text-center small">Tidak ada course.</div>');
        }

        rows.forEach(c => {
            const id = `cgc_${c.courseId}`;
            const checked = selected.has(c.courseId) ? 'checked' : '';
            const disabled = readOnly ? 'disabled' : '';
            const inactive = c.isActive ? '' : ' <span class="badge bg-secondary">inactive</span>';

            $box.append(
                `<label class="d-flex align-items-start gap-2 px-3 py-2 border-bottom mb-0" for="${id}" style="cursor:pointer">
                    <input class="form-check-input mt-1 cg-chk" type="checkbox" id="${id}" data-id="${c.courseId}" ${checked} ${disabled} />
                    <span class="flex-grow-1">
                        <span class="fw-medium">${esc(c.courseCode)}</span> - ${esc(c.courseName)}${inactive}
                    </span>
                    <span class="text-muted small text-nowrap">${esc(c.durationHours)} h</span>
                </label>`);
        });

        $('#cgSelectedCount').text(selected.size);
    }

    function setReadOnly(flag) {
        readOnly = flag;
        $('#cgName, #cgDesc, #cgActive').prop('disabled', flag);
        $('#cgSave').toggle(!flag);
        $('#cgOnlySelectedWrap').toggle(!flag);
    }

    /* ---------- modal ---------- */

    function resetForm() {
        $('#cgId').val('0');
        $('#cgName').val('');
        $('#cgDesc').val('');
        $('#cgActive').prop('checked', true);
        $('#cgSearch').val('');
        $('#cgOnlySelected').prop('checked', false);
        selected.clear();
        courses = [];
    }

    function loadOptions() {
        return $.getJSON(urls.getCourseOptions).then(function (res) {
            if (!res.success) {
                return $.Deferred().reject({ responseJSON: res }).promise();
            }
            return res.data;
        });
    }

    function openNew() {
        resetForm();
        setReadOnly(false);
        $('#cgModalTitle').text('Add Course Group');

        loadOptions()
            .done(function (opts) {
                courses = opts;
                renderCourseList();
                modal.show();
            })
            .fail(ajaxError);
    }

    function openExisting(id, mode) {
        resetForm();
        setReadOnly(mode === 'view');
        $('#cgModalTitle').text(mode === 'view' ? 'Course Group' : 'Edit Course Group');

        const detailReq = $.getJSON(urls.getDetail, { id: id });
        const optionsReq = mode === 'view' ? null : loadOptions();

        $.when(detailReq, optionsReq).done(function (detailRes, opts) {
            const res = detailRes[0];

            if (!res.success) {
                toast('error', res.message || 'Gagal memuat data.');
                return;
            }

            const d = res.data;

            $('#cgId').val(d.courseGroupId);
            $('#cgName').val(d.groupName);
            $('#cgDesc').val(d.description || '');
            $('#cgActive').prop('checked', d.isActive);

            d.courses.forEach(c => selected.add(c.courseId));

            if (mode === 'view') {
                courses = d.courses;
            } else {
                // opts = hasil loadOptions() (array); course nonaktif milik group ini tetap ditampilkan
                const list = Array.isArray(opts) ? opts : [];
                const known = new Set(list.map(c => c.courseId));
                d.courses.filter(c => !known.has(c.courseId)).forEach(c => list.push(c));
                list.sort((a, b) => a.courseCode.localeCompare(b.courseCode));
                courses = list;
            }

            renderCourseList();
            modal.show();
        }).fail(ajaxError);
    }

    /* ---------- save / delete ---------- */

    function save() {
        const name = ($('#cgName').val() || '').trim();

        if (!name) {
            toast('warning', 'Group Name wajib diisi.');
            return;
        }

        if (selected.size === 0) {
            toast('warning', 'Pilih minimal satu course.');
            return;
        }

        const body = new URLSearchParams();
        body.append('__RequestVerificationToken', token());
        body.append('CourseGroupId', $('#cgId').val() || '0');
        body.append('GroupName', name);
        body.append('Description', ($('#cgDesc').val() || '').trim());
        body.append('IsActive', $('#cgActive').is(':checked') ? 'true' : 'false');
        selected.forEach(id => body.append('CourseIds', id));

        const $btn = $('#cgSave').prop('disabled', true);

        $.ajax({
            url: urls.save,
            method: 'POST',
            data: body.toString(),
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8'
        }).done(function (res) {
            if (!res.success) {
                toast('error', res.message);
                return;
            }
            modal.hide();
            toast('success', res.message);
            loadTable();
        }).fail(ajaxError)
          .always(() => $btn.prop('disabled', false));
    }

    function remove(id, name) {
        Swal.fire({
            icon: 'warning',
            title: 'Hapus Course Group?',
            html: `<b>${esc(name)}</b> akan dihapus. Matrix yang sudah dibuat tidak terpengaruh.`,
            showCancelButton: true,
            confirmButtonText: 'Hapus',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            $.ajax({
                url: urls.remove,
                method: 'POST',
                data: { id: id, __RequestVerificationToken: token() }
            }).done(function (res) {
                toast(res.success ? 'success' : 'error', res.message);
                if (res.success) loadTable();
            }).fail(ajaxError);
        });
    }

    /* ---------- init ---------- */

    $(function () {
        modal = new bootstrap.Modal(document.getElementById('cgModal'));

        initTable();
        loadTable();

        $('#btnAddGroup').on('click', openNew);
        $('#cgSave').on('click', save);

        $('#cgTable')
            .on('click', '.cg-view', function () { openExisting($(this).data('id'), 'view'); })
            .on('click', '.cg-edit', function () { openExisting($(this).data('id'), 'edit'); })
            .on('click', '.cg-del', function () { remove($(this).data('id'), $(this).data('name')); });

        $('#cgSearch').on('input', renderCourseList);
        $('#cgOnlySelected').on('change', renderCourseList);

        $('#cgCourseList').on('change', '.cg-chk', function () {
            const id = Number($(this).data('id'));
            if (this.checked) selected.add(id); else selected.delete(id);
            $('#cgSelectedCount').text(selected.size);
        });
    });

})();
