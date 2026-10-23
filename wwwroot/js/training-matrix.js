/* =========================================================
   Training Matrix - daftar Department (Phase 1B-3)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi: window.tmIndexCfg (Views/TrainingMatrix/Index.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const cfg = window.tmIndexCfg;
    const urls = cfg.urls;
    const canEdit = cfg.canEdit === true;

    let table = null;

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
        return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' });
    }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    function statusBadge(status) {
        if (!status) return '<span class="badge bg-light text-dark border">Not created</span>';
        const cls = {
            'Draft': 'bg-secondary',
            'Pending for HRGA Head': 'bg-warning text-dark',
            'Approved': 'bg-success',
            'Rejected': 'bg-danger'
        }[status] || 'bg-secondary';
        return `<span class="badge ${cls}">${esc(status)}</span>`;
    }

    function openDetail(id) {
        window.location.href = `${urls.detail}?id=${encodeURIComponent(id)}`;
    }

    function loadTable() {
        $.getJSON(urls.getList)
            .done(function (res) {
                if (!res.success) {
                    toast('error', res.message || 'Gagal memuat data.');
                    return;
                }
                table.clear().rows.add(res.data).draw();

                const total = res.data.length;
                const created = res.data.filter(r => r.matrixId).length;
                const approved = res.data.filter(r => r.docStatus === 2).length;
                $('#tmSummary').text(`${total} department · ${created} matrix dibuat · ${approved} approved`);
            })
            .fail(ajaxError);
    }

    function createMatrix(abrv) {
        Swal.fire({
            icon: 'question',
            title: 'Buat Training Matrix?',
            html: `Department <b>${esc(abrv)}</b> akan dibuat sebagai Draft.`,
            showCancelButton: true,
            confirmButtonText: 'Buat',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            $.ajax({
                url: urls.create,
                method: 'POST',
                data: { departmentAbrv: abrv, __RequestVerificationToken: token() }
            }).done(function (res) {
                if (!res.success) {
                    toast('error', res.message);
                    return;
                }
                openDetail(res.data.matrixId);
            }).fail(ajaxError);
        });
    }

    function removeMatrix(id, abrv) {
        Swal.fire({
            icon: 'warning',
            title: 'Hapus Training Matrix?',
            html: `Dokumen <b>${esc(abrv)}</b> akan dihapus.`,
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

    $(function () {
        table = $('#tmTable').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 50,
            order: [[0, 'asc']],
            columns: [
                { title: 'Department', data: 'departmentAbrv', render: d => `<span class="fw-medium">${esc(d)}</span>` },
                { title: 'Dept Code', data: 'departmentCodes', render: d => esc(d) },
                { title: 'Sub Dept', data: 'subDepartmentCount', className: 'text-center' },
                { title: 'Status', data: 'status', className: 'text-center', render: d => statusBadge(d) },
                { title: 'Rows', data: 'detailCount', className: 'text-center', render: (d, t, r) => r.matrixId ? d : '' },
                {
                    title: 'Change Request', data: 'activeChangeRequestStatus', className: 'text-center',
                    render: (d, t, r) => d
                        ? `<a href="${urls.changeRequest}?id=${r.activeChangeRequestId}" class="text-decoration-none"><span class="badge bg-info text-dark">${esc(d)}</span></a>`
                        : ''
                },
                {
                    title: 'Last Update', data: null,
                    render: r => r.matrixId
                        ? `${fmtDate(r.modifiedDate || r.createdDate)}<div class="text-muted small">${esc(r.modifiedBy)}</div>`
                        : ''
                },
                {
                    title: 'Action', data: null, orderable: false, className: 'text-center text-nowrap',
                    render: function (r) {
                        if (r.matrixId) {
                            let html = `<button type="button" class="btn btn-sm btn-outline-primary me-1 tm-open" data-id="${r.matrixId}" title="Open"><i class="fas fa-folder-open"></i></button>`;
                            if (canEdit && r.docStatus === 0) {
                                html += `<button type="button" class="btn btn-sm btn-outline-danger tm-del" data-id="${r.matrixId}" data-abrv="${esc(r.departmentAbrv)}" title="Delete"><i class="fas fa-trash"></i></button>`;
                            }
                            return html;
                        }
                        return canEdit
                            ? `<button type="button" class="btn btn-sm btn-primary tm-create" data-abrv="${esc(r.departmentAbrv)}"><i class="bi bi-plus-lg me-1"></i>Create</button>`
                            : '';
                    }
                }
            ]
        });

        $('#tmTable')
            .on('click', '.tm-open', function () { openDetail($(this).data('id')); })
            .on('click', '.tm-create', function () { createMatrix($(this).data('abrv')); })
            .on('click', '.tm-del', function () { removeMatrix($(this).data('id'), $(this).data('abrv')); });

        loadTable();
    });

})();
