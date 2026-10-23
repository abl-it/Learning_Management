/* =========================================================
   Employee Matrix (read-only)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi: window.emCfg (Views/EmployeeMatrix/Index.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const urls = window.emCfg.urls;

    let filters = { companies: [], departments: [], subDepartments: [] };
    let table = null;
    let modal = null;
    let loadSeq = 0;   // mencegah respons lama menimpa respons terbaru

    /* ---------- helpers ---------- */

    function esc(value) { return $('<div>').text(value ?? '').html(); }

    function pad(n) { return String(n).padStart(2, '0'); }

    function fmtDate(value) {
        if (!value) return '';
        const d = new Date(value);
        if (isNaN(d)) return '';
        return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
    }

    function fmtDateTime(value) {
        if (!value) return '';
        const d = new Date(value);
        if (isNaN(d)) return '';
        return `${fmtDate(value)} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
    }

    function period(start, end) {
        if (!start) return '';
        if (!end) return fmtDateTime(start);
        const s = new Date(start), e = new Date(end);
        if (!isNaN(s) && !isNaN(e) && s.toDateString() === e.toDateString()) {
            return `${fmtDate(start)} ${pad(s.getHours())}:${pad(s.getMinutes())}-${pad(e.getHours())}:${pad(e.getMinutes())}`;
        }
        return `${fmtDateTime(start)} - ${fmtDateTime(end)}`;
    }

    function toast(icon, text) { return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' }); }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    function statusBadge(s) {
        return s === 'Included' ? '<span class="badge bg-success">Included</span>'
            : s === 'Unmapped' ? '<span class="badge bg-warning text-dark">Unmapped</span>'
            : '<span class="badge bg-secondary">No Matrix</span>';
    }

    function reqBadge(r) {
        return r === 'Mandatory'
            ? '<span class="badge bg-danger">Mandatory</span>'
            : '<span class="badge bg-info text-dark">Recommended</span>';
    }

    function eventBadge(s) {
        const cls = { 'Complete': 'bg-success', 'Approved': 'bg-primary', 'Canceled': 'bg-danger', 'Rejected': 'bg-danger' }[s] || 'bg-secondary';
        return `<span class="badge ${cls}">${esc(s)}</span>`;
    }

    function attendBadge(s) {
        const v = (s || '').toUpperCase();
        const cls = v === 'PRESENT' ? 'bg-success' : (v === 'ABSENT' ? 'bg-danger' : 'bg-secondary');
        return `<span class="badge ${cls}">${esc(s)}</span>`;
    }

    /* ---------- filters ---------- */

    function company() { return $('#emCompany').val() || 'ABL'; }

    /** Company hanya yang punya Department yang boleh dilihat pengguna. */
    function fillCompanies() {
        const list = filters.companies || [];
        if (list.length === 0) return;

        const current = $('#emCompany').val();
        const $c = $('#emCompany').empty();
        list.forEach(c => $c.append(`<option value="${esc(c)}">${esc(c)}</option>`));
        $c.val(list.includes(current) ? current : list[0]);
    }

    function fillDepartments() {
        const $d = $('#emDept').empty();

        const depts = filters.departments
            .filter(d => d.company === company())
            .sort((a, b) => a.name.localeCompare(b.name));

        // "All Department" hanya bila ada lebih dari satu Department yang boleh dipilih.
        if (depts.length !== 1) {
            $d.append('<option value="">All Department</option>');
        }

        depts.forEach(d => $d.append(`<option value="${esc(d.abrv)}">${esc(d.name)}</option>`));

        fillSubDepartments();
    }

    function fillSubDepartments() {
        const abrv = $('#emDept').val();
        const $s = $('#emSub').empty().append('<option value="">All Sub Department</option>');

        if (!abrv) {
            $s.prop('disabled', true);
            return;
        }

        filters.subDepartments
            .filter(s => s.abrv === abrv)
            .forEach(s => $s.append(`<option value="${s.id}">${esc(s.name)}</option>`));

        $s.prop('disabled', false);
    }

    /* ---------- list ---------- */

    function loadEmployees() {
        const seq = ++loadSeq;

        $.getJSON(urls.employees, {
            company: company(),
            abrv: $('#emDept').val() || '',
            subDepartmentId: $('#emSub').val() || ''
        }).done(function (res) {
            if (seq !== loadSeq) return;

            if (!res.success) {
                toast('error', res.message || 'Gagal memuat data.');
                return;
            }

            table.clear().rows.add(res.data).draw();
            $('#emCount').text(`${res.data.length} karyawan`);
        }).fail(ajaxError);
    }

    function ratio(done, req) { return req > 0 ? `${done}/${req}` : '–'; }

    function initTable() {
        table = $('#emTable').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 25,
            order: [[1, 'asc']],
            columns: [
                { title: 'Kode', data: 'employeeCode', width: '8%' },
                { title: 'Nama', data: 'name', width: '20%', render: d => `<span class="fw-medium" title="${esc(d)}">${esc(d)}</span>` },
                { title: 'Department', data: 'departmentName', width: '18%', render: d => `<span title="${esc(d)}">${esc(d)}</span>` },
                { title: 'Sub Department', data: 'subDepartmentName', width: '20%', render: d => `<span title="${esc(d)}">${esc(d)}</span>` },
                { title: 'Layer', data: 'layer', width: '6%', className: 'text-center', render: d => d ?? '–' },
                { title: 'Status', data: 'status', width: '10%', className: 'text-center', render: d => statusBadge(d) },
                {
                    title: 'Mandatory', data: null, width: '8%', className: 'text-center',
                    render: (r, t) => t === 'display' ? ratio(r.mandatoryFulfilled, r.mandatoryRequired) : r.mandatoryRequired - r.mandatoryFulfilled
                },
                {
                    title: 'Recomm.', data: null, width: '8%', className: 'text-center',
                    render: (r, t) => t === 'display' ? ratio(r.recommendedFulfilled, r.recommendedRequired) : r.recommendedRequired - r.recommendedFulfilled
                },
                {
                    title: '', data: null, width: '5%', orderable: false, className: 'text-center',
                    render: r => `<button type="button" class="btn btn-sm btn-outline-primary em-open" data-code="${esc(r.employeeCode)}" title="Detail"><i class="fas fa-eye"></i></button>`
                }
            ]
        });
    }

    /* ---------- detail ---------- */

    function openDetail(code) {
        $.getJSON(urls.detail, { employeeCode: code })
            .done(function (res) {
                if (!res.success) { toast('error', res.message || 'Gagal memuat data.'); return; }
                renderDetail(res.data);
                modal.show();
            })
            .fail(ajaxError);
    }

    function pill(label, value, cls) {
        return `<div class="border rounded px-3 py-2"><div class="text-muted small">${esc(label)}</div><div class="fw-semibold ${cls || ''}">${value}</div></div>`;
    }

    function renderDetail(d) {
        $('#emTitle').text(`${d.employeeCode} - ${d.name}`);

        const where = [d.departmentName, d.subDepartmentName].filter(Boolean).join(' / ');
        $('#emSubtitle').html(
            `${esc(where)}${d.layer ? ` · Layer ${d.layer}` : ''} ${statusBadge(d.status)}` +
            `${d.matrixStatus ? ` <span class="text-muted">· Matrix ${esc(d.matrixStatus)}</span>` : ''}`);

        let notice = '';
        if (d.status === 'Unmapped') {
            notice = `<div class="alert alert-warning py-2">Karyawan belum dapat dinilai: <b>${esc(d.reason)}</b>. Lengkapi data di HRMS agar Layer dan course matrix dapat ditentukan.</div>`;
        } else if (d.status === 'No Matrix') {
            notice = '<div class="alert alert-info py-2">Matrix Department ini belum Approved, sehingga course yang harus dilakukan belum dapat ditentukan.</div>';
        }
        $('#emNotice').html(notice);

        if (d.status === 'Included') {
            const mandCls = d.mandatoryRequired > 0 && d.mandatoryFulfilled < d.mandatoryRequired ? 'text-danger' : 'text-success';
            $('#emSummary').html(
                pill('Mandatory terpenuhi', `${d.mandatoryFulfilled} / ${d.mandatoryRequired}`, mandCls) +
                pill('Recommended terpenuhi', `${d.recommendedFulfilled} / ${d.recommendedRequired}`));
        } else {
            $('#emSummary').empty();
        }

        // Matrix
        $('#emMatrixCount').text(d.required.length);

        const matrixRows = d.required.map(r => {
            const status = r.fulfilled
                ? `<span class="badge bg-success">Terpenuhi</span> <span class="small text-muted">${esc(r.fulfilledEventCode)} · ${fmtDate(r.fulfilledDate)}</span>`
                : '<span class="badge bg-secondary">Belum</span>';

            return `<tr><td style="width:12%">${esc(r.courseCode)}</td><td>${esc(r.courseName)}</td>` +
                `<td class="text-center" style="width:14%">${reqBadge(r.requirement)}</td>` +
                `<td style="width:30%">${status}</td></tr>`;
        }).join('');

        $('#emMatrixTable').html(
            '<thead class="table-dark"><tr><th>Kode</th><th>Course</th><th class="text-center">Requirement</th><th>Status</th></tr></thead>' +
            `<tbody>${matrixRows || '<tr><td colspan="4" class="text-center text-muted py-3">Tidak ada course pada matrix untuk Sub Department dan Layer ini.</td></tr>'}</tbody>`);

        // Aktual
        $('#emActualCount').text(d.history.length);

        const actualRows = d.history.map(h =>
            `<tr>
                <td class="text-nowrap">${esc(h.trainingEventId)}</td>
                <td class="text-nowrap">${esc(h.eventCode)}</td>
                <td>${esc(h.courseCode)}${h.trainingTitle ? ` - ${esc(h.trainingTitle)}` : ''}</td>
                <td class="text-nowrap">${esc(period(h.eventStartDate, h.eventEndDate))}${h.realizationDate ? `<div class="small text-muted">Realisasi ${fmtDate(h.realizationDate)}</div>` : ''}</td>
                <td class="text-center">${eventBadge(h.eventStatus)}</td>
                <td class="text-center">${attendBadge(h.attendanceStatus)}</td>
                <td>${esc(h.trainerName)}${h.venue ? `<div class="small text-muted">${esc(h.venue)}</div>` : ''}</td>
                <td class="text-center">${h.countsForCompliance ? '<i class="fas fa-circle-check text-success" title="Dihitung"></i>' : ''}</td>
            </tr>`).join('');

        $('#emActualTable').html(
            '<thead class="table-dark"><tr><th>Event ID</th><th>Event Code</th><th>Training</th><th>Tanggal</th><th class="text-center">Status Event</th><th class="text-center">Kehadiran</th><th>Trainer / Venue</th><th class="text-center">Dihitung</th></tr></thead>' +
            `<tbody>${actualRows || '<tr><td colspan="8" class="text-center text-muted py-3">Belum ada training aktual.</td></tr>'}</tbody>`);
    }

    /* ---------- export ---------- */

    function exportExcel() {
        const $btn = $('#emExport').prop('disabled', true);

        const query = $.param({
            company: company(),
            abrv: $('#emDept').val() || '',
            subDepartmentId: $('#emSub').val() || ''
        });

        fetch(`${urls.exportMatrix}?${query}`, { credentials: 'same-origin' })
            .then(async function (resp) {
                const type = resp.headers.get('Content-Type') || '';

                // Gagal: server mengembalikan JSON {success:false,message}
                if (!resp.ok || type.includes('application/json')) {
                    let message = 'Export gagal.';
                    try { message = (await resp.json()).message || message; } catch (e) { /* abaikan */ }
                    throw new Error(message);
                }

                const disposition = resp.headers.get('Content-Disposition') || '';
                const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
                const fileName = match ? decodeURIComponent(match[1]) : 'TrainingMatrix.xlsx';

                return resp.blob().then(blob => ({ blob, fileName }));
            })
            .then(function (r) {
                const url = URL.createObjectURL(r.blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = r.fileName;
                document.body.appendChild(a);
                a.click();
                a.remove();
                URL.revokeObjectURL(url);
            })
            .catch(err => toast('error', err.message))
            .finally(() => $btn.prop('disabled', false));
    }

    /* ---------- init ---------- */

    $(function () {
        modal = new bootstrap.Modal(document.getElementById('emModal'));

        initTable();

        $.getJSON(urls.filters)
            .done(function (res) {
                if (!res.success) { toast('error', res.message || 'Gagal memuat filter.'); return; }

                filters = res.data;
                fillCompanies();
                fillDepartments();
                loadEmployees();
            })
            .fail(ajaxError);

        $('#emCompany').on('change', function () { fillDepartments(); loadEmployees(); });
        $('#emDept').on('change', function () { fillSubDepartments(); loadEmployees(); });
        $('#emSub').on('change', loadEmployees);
        $('#emExport').on('click', exportExcel);

        $('#emTable tbody').on('click', 'tr', function () {
            const row = table.row(this).data();
            if (row) openDetail(row.employeeCode);
        });
    });

})();
