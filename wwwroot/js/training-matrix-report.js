/* =========================================================
   Training Matrix Compliance Report (Phase 1C-1)
   Dependensi: jQuery, DataTables, Bootstrap 5, SweetAlert2
   Konfigurasi: window.tmReportCfg (Views/TrainingMatrixReport/Index.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const urls = window.tmReportCfg.urls;

    let deptTable = null;
    let reasonTable = null;
    let unmappedTable = null;
    let empTable = null;
    let empModal = null;
    let unmappedLoaded = false;

    /* ---------- helpers ---------- */

    function esc(value) { return $('<div>').text(value ?? '').html(); }

    function pct(v) { return v === null || v === undefined ? '–' : `${v.toFixed(1)}%`; }

    function fmtDate(value) {
        const d = new Date(value);
        if (isNaN(d)) return '';
        const p = n => String(n).padStart(2, '0');
        return `${p(d.getDate())}/${p(d.getMonth() + 1)}/${d.getFullYear()} ${p(d.getHours())}:${p(d.getMinutes())}`;
    }

    function toast(icon, text) { return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' }); }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    function barClass(v) {
        if (v === null || v === undefined) return 'bg-secondary';
        if (v >= 90) return 'bg-success';
        if (v >= 70) return 'bg-warning';
        return 'bg-danger';
    }

    function progress(v) {
        if (v === null || v === undefined) return '<span class="text-muted">–</span>';
        return `<div class="d-flex align-items-center gap-2" style="min-width:100px">
                    <div class="progress flex-grow-1" style="height:8px"><div class="progress-bar ${barClass(v)}" style="width:${Math.min(v, 100)}%"></div></div>
                    <span class="small fw-medium">${pct(v)}</span>
                </div>`;
    }

    function matrixBadge(status) {
        const cls = {
            'Approved': 'bg-success',
            'Draft': 'bg-secondary',
            'Pending for HRGA Head': 'bg-warning text-dark',
            'Rejected': 'bg-danger'
        }[status] || 'bg-light text-dark border';
        return `<span class="badge ${cls}">${esc(status)}</span>`;
    }

    /* ---------- summary ---------- */

    function loadSummary() {
        $.getJSON(urls.summary)
            .done(function (res) {
                if (!res.success) {
                    toast('error', res.message || 'Gagal memuat laporan.');
                    return;
                }

                const d = res.data;

                $('#rpGenerated').text(`Dihitung ${fmtDate(d.generatedAt)}`);

                $('#kpiCompliance').text(pct(d.compliancePercent));
                $('#kpiComplianceSub').text(`${d.mandatoryFulfilled} dari ${d.mandatoryRequired} course Mandatory · ${d.includedEmployees} karyawan`);
                $('#kpiGap').text(d.employeesWithGap);
                $('#kpiGapSub').text(`dari ${d.includedEmployees} karyawan yang dihitung`);
                $('#kpiUnmapped').text(pct(d.unmappedPercent));
                $('#kpiUnmappedSub').text(`${d.unmappedEmployees} dari ${d.totalEmployees} karyawan aktif`);
                $('#kpiNoMatrix').text(d.noMatrixEmployees);

                deptTable.clear().rows.add(d.departments).draw();
                deptTable.columns.adjust();
                reasonTable.clear().rows.add(d.unmappedReasons).draw();
            })
            .fail(ajaxError);
    }

    function loadUnmapped() {
        $.getJSON(urls.unmapped)
            .done(function (res) {
                if (!res.success) { toast('error', res.message || 'Gagal memuat data.'); return; }
                unmappedTable.clear().rows.add(res.data).draw();
                unmappedTable.columns.adjust();
                unmappedLoaded = true;
            })
            .fail(ajaxError);
    }

    /* ---------- drill-down ---------- */

    function openEmployees(abrv) {
        $('#rpEmpTitle').text(abrv);
        empTable.clear().draw();

        $.getJSON(urls.employees, { abrv: abrv })
            .done(function (res) {
                if (!res.success) { toast('error', res.message || 'Gagal memuat data.'); return; }
                empTable.rows.add(res.data).draw();
                empModal.show();
            })
            .fail(ajaxError);
    }

    function gapHtml(r) {
        if (r.status === 'Unmapped') {
            return `<div class="p-2 text-muted">Unmapped: ${esc(r.reason)}</div>`;
        }
        if (r.status === 'No Matrix') {
            return '<div class="p-2 text-muted">Department belum punya matrix Approved.</div>';
        }
        if (!r.gaps || r.gaps.length === 0) {
            return '<div class="p-2 text-success">Semua course sudah terpenuhi.</div>';
        }
        const rows = r.gaps.map(g =>
            `<tr><td>${esc(g.courseCode)}</td><td>${esc(g.courseName)}</td>
             <td>${g.requirement === 'Mandatory'
                ? '<span class="badge bg-danger">Mandatory</span>'
                : '<span class="badge bg-info text-dark">Recommended</span>'}</td></tr>`).join('');
        return `<table class="table table-sm mb-0"><thead><tr><th>Kode</th><th>Course belum terpenuhi</th><th>Requirement</th></tr></thead><tbody>${rows}</tbody></table>`;
    }

    /* ---------- init ---------- */

    $(function () {
        empModal = new bootstrap.Modal(document.getElementById('rpEmpModal'));

        deptTable = $('#rpDept').DataTable({
            data: [],
            autoWidth: false,
            pageLength: 50,
            order: [[0, 'asc']],
            columns: [
                { title: 'Department', data: 'departmentAbrv', width: '16%', className: 'text-nowrap', render: d => `<span class="fw-medium">${esc(d)}</span>` },
                { title: 'Matrix', data: 'matrixStatus', width: '15%', className: 'text-center text-nowrap', render: d => matrixBadge(d) },
                { title: 'Karyawan', data: 'employeeCount', width: '8%', className: 'text-center' },
                { title: 'Dihitung', data: 'includedCount', width: '8%', className: 'text-center' },
                { title: 'Unmapped', data: 'unmappedCount', width: '8%', className: 'text-center' },
                { title: 'Mandatory %', data: 'compliancePercent', width: '17%', render: (d, t) => t === 'display' ? progress(d) : (d ?? -1) },
                { title: 'Gap', data: 'employeesWithGap', width: '6%', className: 'text-center' },
                { title: 'Recomm. %', data: 'recommendedPercent', width: '10%', className: 'text-center', render: (d, t) => t === 'display' ? pct(d) : (d ?? -1) },
                {
                    title: 'Action', data: null, width: '12%', orderable: false, className: 'text-center text-nowrap',
                    render: function (r) {
                        let h = `<button type="button" class="btn btn-sm btn-outline-primary me-1 rp-emp" data-abrv="${esc(r.departmentAbrv)}" title="Karyawan"><i class="fas fa-users"></i></button>`;
                        if (r.matrixId) {
                            h += `<a class="btn btn-sm btn-outline-secondary" href="${urls.matrixDetail}?id=${r.matrixId}" title="Buka matrix"><i class="fas fa-table-cells"></i></a>`;
                        }
                        return h;
                    }
                }
            ]
        });

        reasonTable = $('#rpReasons').DataTable({
            data: [],
            paging: false, searching: false, info: false, ordering: false,
            columns: [
                { title: 'Alasan', data: 'reason', render: d => esc(d) },
                { title: 'Jumlah', data: 'count', className: 'text-end' }
            ]
        });

        unmappedTable = $('#rpUnmapped').DataTable({
            data: [],
            autoWidth: true,
            pageLength: 25,
            columns: [
                { title: 'Kode', data: 'employeeCode', className: 'text-nowrap', render: d => esc(d) },
                { title: 'Nama', data: 'name', render: d => esc(d) },
                {
                    title: 'Department', data: null,
                    render: r => r.departmentAbrv
                        ? esc(r.departmentName ? `${r.departmentAbrv} - ${r.departmentName}` : r.departmentAbrv)
                        : ''
                },
                {
                    title: 'Sub Department', data: null,
                    render: r => r.subDepartmentName
                        ? esc(r.subDepartmentName)
                        : (r.subDepartmentId ? `<span class="text-muted">ID ${esc(r.subDepartmentId)} (tidak dikenal)</span>` : '')
                },
                { title: 'PositionID', data: 'positionId', className: 'text-center' },
                { title: 'JobGrade', data: 'jobGradeId', className: 'text-center' },
                { title: 'Alasan', data: 'reason', className: 'text-nowrap', render: d => esc(d) }
            ]
        });

        empTable = $('#rpEmp').DataTable({
            data: [],
            autoWidth: true,
            pageLength: 25,
            order: [],
            columns: [
                { title: 'Kode', data: 'employeeCode', render: d => esc(d) },
                { title: 'Nama', data: 'name', render: d => esc(d) },
                { title: 'Sub Department', data: null, render: r => r.subDepartmentCode ? `${esc(r.subDepartmentCode)} - ${esc(r.subDepartmentName)}` : '' },
                { title: 'Layer', data: 'layer', className: 'text-center', render: d => d ?? '–' },
                {
                    title: 'Status', data: 'status', className: 'text-center',
                    render: d => d === 'Included' ? '<span class="badge bg-success">Included</span>'
                        : d === 'Unmapped' ? '<span class="badge bg-warning text-dark">Unmapped</span>'
                        : '<span class="badge bg-secondary">No Matrix</span>'
                },
                {
                    title: 'Mandatory', data: null, className: 'text-center',
                    render: r => r.status === 'Included' ? `${r.mandatoryFulfilled}/${r.mandatoryRequired}` : ''
                },
                {
                    title: 'Recommended', data: null, className: 'text-center',
                    render: r => r.status === 'Included' ? `${r.recommendedFulfilled}/${r.recommendedRequired}` : ''
                }
            ]
        });

        document.getElementById('rpEmpModal').addEventListener('shown.bs.modal', function () { empTable.columns.adjust(); });

        $('#rpDept').on('click', '.rp-emp', function () { openEmployees($(this).data('abrv')); });

        // expand gap per karyawan
        $('#rpEmp tbody').on('click', 'tr', function () {
            const row = empTable.row(this);
            if (!row.data()) return;

            if (row.child.isShown()) {
                row.child.hide();
            } else {
                row.child(gapHtml(row.data())).show();
            }
        });

        $('#tabQualityBtn').on('shown.bs.tab', function () {
            if (!unmappedLoaded) loadUnmapped();
        });

        $('#rpRefresh').on('click', function () {
            unmappedLoaded = false;
            loadSummary();
            if ($('#tabQuality').hasClass('active')) loadUnmapped();
        });

        loadSummary();
    });

})();
