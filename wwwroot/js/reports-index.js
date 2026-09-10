(function () {
    'use strict';

    const state = {
        activeTab: 'department',
        companies: [],
        departments: [],
        lastRows: [],
        lastFilter: {}
    };

    function qs(id) {
        return document.getElementById(id);
    }

    function formatHours(value) {
        const n = Number(value) || 0;
        return n.toLocaleString(undefined, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    }

    function formatInt(value) {
        return (Number(value) || 0).toLocaleString();
    }

    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function buildQuery(params) {
        const parts = [];

        Object.keys(params).forEach(function (key) {
            const value = params[key];
            if (value !== null && value !== undefined && value !== '') {
                parts.push(encodeURIComponent(key) + '=' + encodeURIComponent(value));
            }
        });

        return parts.join('&');
    }

    function currentFilter() {
        return {
            CoCode: qs('filterCoCode').value,
            ABRV: qs('filterABRV').value,
            StartDate: qs('filterStartDate').value,
            EndDate: qs('filterEndDate').value
        };
    }

    function loadCompanies() {
        fetch(window.reportsUrls.companies)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                state.companies = data || [];

                const select = qs('filterCoCode');

                state.companies.forEach(function (item) {
                    const option = document.createElement('option');
                    option.value = item.value;
                    option.textContent = item.text;
                    select.appendChild(option);
                });
            })
            .catch(function () {
                console.error('[Reports] Failed to load companies.');
            });
    }

    function loadDepartments(coCode) {
        const url = window.reportsUrls.departments +
            (coCode ? '?coCode=' + encodeURIComponent(coCode) : '');

        return fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                state.departments = data || [];

                const select = qs('filterABRV');
                const previousValue = select.value;

                select.innerHTML = '<option value="">All Departments</option>';

                state.departments.forEach(function (item) {
                    const option = document.createElement('option');
                    option.value = item.value;
                    option.textContent = item.text;
                    select.appendChild(option);
                });

                // Keep the previous selection if it's still valid for this company.
                if (state.departments.some(function (d) { return d.value === previousValue; })) {
                    select.value = previousValue;
                }
            })
            .catch(function () {
                console.error('[Reports] Failed to load departments.');
            });
    }

    function setLoading(isLoading) {
        qs('inquiryLoading').classList.toggle('d-none', !isLoading);

        if (isLoading) {
            qs('inquiryNoResults').classList.add('d-none');
        }
    }

    function renderDepartmentTable(rows) {
        const body = qs('tableByDepartmentBody');
        body.innerHTML = '';

        let totalParticipants = 0;
        let totalHours = 0;

        rows.forEach(function (row) {
            totalParticipants += Number(row.totalParticipants) || 0;
            totalHours += Number(row.totalHours) || 0;

            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + escapeHtml(row.coCode || '-') + '</td>' +
                '<td>' + escapeHtml(row.deptName || row.abrv || '-') + '</td>' +
                '<td class="text-end">' + formatInt(row.totalEvents) + '</td>' +
                '<td class="text-end">' + formatInt(row.totalParticipants) + '</td>' +
                '<td class="text-end">' + formatHours(row.totalHours) + '</td>';
            body.appendChild(tr);
        });

        qs('summaryLabel1').textContent = 'Departments';
        qs('summaryTotalEvents').textContent = formatInt(rows.length);
        qs('summaryLabel2').textContent = 'Total Participants';
        qs('summaryTotalParticipants').textContent = formatInt(totalParticipants);
        qs('summaryTotalHours').textContent = formatHours(totalHours);

        return rows.length;
    }

    function renderPersonTable(rows) {
        const body = qs('tableByPersonBody');
        body.innerHTML = '';

        let totalTrainings = 0;
        let totalHours = 0;

        rows.forEach(function (row) {
            totalTrainings += Number(row.totalTrainings) || 0;
            totalHours += Number(row.totalHours) || 0;

            const roleBadgeClass = row.role === 'Instructor' ? 'bg-primary' : 'bg-secondary';

            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + escapeHtml(row.employeeCode) + '</td>' +
                '<td>' + escapeHtml(row.name) + '</td>' +
                '<td>' + escapeHtml(row.deptName || row.abrv || '-') + '</td>' +
                '<td><span class="badge ' + roleBadgeClass + '">' + escapeHtml(row.role) + '</span></td>' +
                '<td class="text-end">' + formatInt(row.totalTrainings) + '</td>' +
                '<td class="text-end">' + formatHours(row.totalHours) + '</td>';
            body.appendChild(tr);
        });

        const uniquePeople = new Set(rows.map(function (row) { return row.employeeCode; })).size;

        qs('summaryLabel1').textContent = 'People';
        qs('summaryTotalEvents').textContent = formatInt(uniquePeople);
        qs('summaryLabel2').textContent = 'Total Trainings';
        qs('summaryTotalParticipants').textContent = formatInt(totalTrainings);
        qs('summaryTotalHours').textContent = formatHours(totalHours);

        return rows.length;
    }

    function loadActiveTab() {

        setLoading(true);

        const filter = currentFilter();
        const query = buildQuery(filter);

        const isDepartment = state.activeTab === 'department';
        const url = (isDepartment ? window.reportsUrls.byDepartment : window.reportsUrls.byPerson) +
            (query ? '?' + query : '');

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (response) {

                setLoading(false);

                const rows = (response && response.data) || [];

                state.lastRows = rows;
                state.lastFilter = filter;

                const count = isDepartment
                    ? renderDepartmentTable(rows)
                    : renderPersonTable(rows);

                qs('inquiryNoResults').classList.toggle('d-none', count > 0);
            })
            .catch(function () {
                setLoading(false);
                console.error('[Reports] Failed to load inquiry data.');
            });
    }

    function switchTab(tab) {

        state.activeTab = tab;

        qs('tabByDepartment').classList.toggle('active', tab === 'department');
        qs('tabByPerson').classList.toggle('active', tab === 'person');

        qs('panelByDepartment').classList.toggle('d-none', tab !== 'department');
        qs('panelByPerson').classList.toggle('d-none', tab !== 'person');

        loadActiveTab();
    }

    function exportToExcel() {

        if (!state.lastRows || state.lastRows.length === 0) {
            alert('No data to export. Please adjust your filters and try again.');
            return;
        }

        if (typeof XLSX === 'undefined') {
            alert('Export library failed to load. Please check your internet connection and try again.');
            return;
        }

        const isDepartment = state.activeTab === 'department';
        let sheetData;
        let sheetName;

        if (isDepartment) {

            sheetName = 'By Department';

            sheetData = state.lastRows.map(function (row) {
                return {
                    'Company': row.coCode || '',
                    'Department': row.deptName || row.abrv || '',
                    'Total Events': Number(row.totalEvents) || 0,
                    'Total Participants': Number(row.totalParticipants) || 0,
                    'Total Hours': Math.round((Number(row.totalHours) || 0) * 10) / 10
                };
            });

        } else {

            sheetName = 'By Person';

            sheetData = state.lastRows.map(function (row) {
                return {
                    'Employee Code': row.employeeCode || '',
                    'Name': row.name || '',
                    'Department': row.deptName || row.abrv || '',
                    'Role': row.role || '',
                    'Total Trainings': Number(row.totalTrainings) || 0,
                    'Total Hours': Math.round((Number(row.totalHours) || 0) * 10) / 10
                };
            });
        }

        const worksheet = XLSX.utils.json_to_sheet(sheetData);

        // Reasonable column widths so the file doesn't open all-squeezed.
        worksheet['!cols'] = Object.keys(sheetData[0]).map(function (key) {
            return { wch: Math.max(key.length, 14) };
        });

        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);

        const today = new Date();
        const pad = function (n) { return n < 10 ? '0' + n : n; };
        const dateStr = today.getFullYear() + '-' + pad(today.getMonth() + 1) + '-' + pad(today.getDate());

        const fileName =
            'TrainingInquiry_' + (isDepartment ? 'ByDepartment' : 'ByPerson') + '_' + dateStr + '.xlsx';

        XLSX.writeFile(workbook, fileName);
    }

    document.addEventListener('DOMContentLoaded', function () {

        loadCompanies();
        loadDepartments(null).then(loadActiveTab);

        qs('filterCoCode').addEventListener('change', function () {
            loadDepartments(this.value).then(loadActiveTab);
        });

        qs('btnApplyFilter').addEventListener('click', loadActiveTab);

        qs('btnExportExcel').addEventListener('click', exportToExcel);

        qs('tabByDepartment').addEventListener('click', function () {
            switchTab('department');
        });

        qs('tabByPerson').addEventListener('click', function () {
            switchTab('person');
        });
    });

})();
