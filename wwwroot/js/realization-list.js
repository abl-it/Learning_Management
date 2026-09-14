(function () {
    'use strict';

    function normalize(value) {
        return (value || '').toString().trim().toLowerCase();
    }

    function applyFilters() {

        const searchInput = document.getElementById('realizationSearchInput');
        const statusFilter = document.getElementById('realizationStatusFilter');
        const companyFilter = document.getElementById('realizationCompanyFilter');
        const deptFilter = document.getElementById('realizationDeptFilter');
        const noResults = document.getElementById('realizationNoResults');

        if (!searchInput || !statusFilter || !companyFilter || !deptFilter) {
            return;
        }

        const searchTerm = normalize(searchInput.value).trim();
        const status = statusFilter.value;
        const coCode = companyFilter.value;
        const abrv = deptFilter.value;

        const rows = document.querySelectorAll('.realization-row');
        let visibleCount = 0;

        rows.forEach(function (row) {

            const matchesSearch =
                searchTerm === '' || normalize(row.getAttribute('data-search')).indexOf(searchTerm) !== -1;

            const matchesStatus =
                status === '' || row.getAttribute('data-status') === status;

            const matchesCompany =
                coCode === '' || normalize(row.getAttribute('data-cocode')) === normalize(coCode);

            const matchesDept =
                abrv === '' || normalize(row.getAttribute('data-abrv')) === normalize(abrv);

            const isMatch = matchesSearch && matchesStatus && matchesCompany && matchesDept;

            // Table rows need their native display (table-row); cards use block.
            row.style.display = isMatch
                ? (row.tagName === 'TR' ? 'table-row' : 'block')
                : 'none';

            if (isMatch) {
                visibleCount++;
            }
        });

        if (noResults) {
            // rows.length counts BOTH the table <tr> set and the mobile card
            // set for every item, so "nothing visible at all" means
            // visibleCount is 0 out of a non-zero total.
            noResults.classList.toggle('d-none', visibleCount > 0 || rows.length === 0);
        }
    }

    function loadCompanies() {

        const $ddl = document.getElementById('realizationCompanyFilter');

        if (!$ddl || typeof realizationFilterUrls === 'undefined') {
            return;
        }

        fetch(realizationFilterUrls.getCompanies)
            .then(function (res) { return res.json(); })
            .then(function (companies) {

                if (!Array.isArray(companies)) {
                    companies = [];
                }

                $ddl.innerHTML = '';

                if (companies.length === 0) {
                    $ddl.innerHTML = '<option value="">No Company</option>';
                    loadDepartments();
                    return;
                }

                if (companies.length === 1) {
                    const item = companies[0];
                    const value = item.value ?? item.Value ?? '';
                    const text = item.text ?? item.Text ?? value;

                    const opt = document.createElement('option');
                    opt.value = value;
                    opt.textContent = text;
                    $ddl.appendChild(opt);
                    $ddl.value = value;

                    loadDepartments();
                    return;
                }

                $ddl.innerHTML = '<option value="">All Company</option>';

                companies.forEach(function (item) {

                    const value = item.value ?? item.Value ?? '';
                    const text = item.text ?? item.Text ?? value;

                    const opt = document.createElement('option');
                    opt.value = value;
                    opt.textContent = text;
                    $ddl.appendChild(opt);
                });

                loadDepartments();
            })
            .catch(function () {
                loadDepartments();
            });
    }

    function loadDepartments() {

        const $ddl = document.getElementById('realizationDeptFilter');
        const $company = document.getElementById('realizationCompanyFilter');

        if (!$ddl || typeof realizationFilterUrls === 'undefined') {
            return;
        }

        const coCode = $company ? $company.value : '';

        const url = coCode
            ? realizationFilterUrls.getDepartments + (realizationFilterUrls.getDepartments.indexOf('?') === -1 ? '?' : '&') + 'coCode=' + encodeURIComponent(coCode)
            : realizationFilterUrls.getDepartments;

        fetch(url)
            .then(function (res) { return res.json(); })
            .then(function (departments) {

                if (!Array.isArray(departments)) {
                    departments = [];
                }

                $ddl.innerHTML = '';

                if (departments.length === 0) {
                    $ddl.innerHTML = '<option value="">No Department</option>';
                    applyFilters();
                    return;
                }

                if (departments.length === 1) {
                    const item = departments[0];
                    const value = item.value ?? item.Value ?? '';
                    const text = item.text ?? item.Text ?? value;

                    const opt = document.createElement('option');
                    opt.value = value;
                    opt.textContent = text;
                    $ddl.appendChild(opt);
                    $ddl.value = value;

                    applyFilters();
                    return;
                }

                $ddl.innerHTML = '<option value="">All Department</option>';

                departments.forEach(function (item) {

                    const value = item.value ?? item.Value ?? '';
                    const text = item.text ?? item.Text ?? value;

                    const opt = document.createElement('option');
                    opt.value = value;
                    opt.textContent = text;
                    $ddl.appendChild(opt);
                });

                applyFilters();
            })
            .catch(function () {
                applyFilters();
            });
    }

    document.addEventListener('DOMContentLoaded', function () {

        const searchInput = document.getElementById('realizationSearchInput');
        const statusFilter = document.getElementById('realizationStatusFilter');
        const companyFilter = document.getElementById('realizationCompanyFilter');
        const deptFilter = document.getElementById('realizationDeptFilter');

        if (searchInput) {
            searchInput.addEventListener('input', applyFilters);
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', applyFilters);
        }

        if (companyFilter) {
            // Company change cascades into Department, then re-applies filters.
            companyFilter.addEventListener('change', function () {
                loadDepartments();
            });
        }

        if (deptFilter) {
            deptFilter.addEventListener('change', applyFilters);
        }

        loadCompanies();
    });

})();
