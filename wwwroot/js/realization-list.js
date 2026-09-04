(function () {
    'use strict';

    function normalize(value) {
        return (value || '').toString().toLowerCase();
    }

    function applyFilters() {

        const searchInput = document.getElementById('realizationSearchInput');
        const statusFilter = document.getElementById('realizationStatusFilter');
        const deptFilter = document.getElementById('realizationDeptFilter');
        const noResults = document.getElementById('realizationNoResults');

        if (!searchInput || !statusFilter || !deptFilter) {
            return;
        }

        const searchTerm = normalize(searchInput.value).trim();
        const status = statusFilter.value;
        const dept = deptFilter.value;

        const rows = document.querySelectorAll('.realization-row');
        let visibleCount = 0;

        rows.forEach(function (row) {

            const matchesSearch =
                searchTerm === '' || normalize(row.getAttribute('data-search')).indexOf(searchTerm) !== -1;

            const matchesStatus =
                status === '' || row.getAttribute('data-status') === status;

            const matchesDept =
                dept === '' || row.getAttribute('data-dept') === dept;

            const isMatch = matchesSearch && matchesStatus && matchesDept;

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

    document.addEventListener('DOMContentLoaded', function () {

        const searchInput = document.getElementById('realizationSearchInput');
        const statusFilter = document.getElementById('realizationStatusFilter');
        const deptFilter = document.getElementById('realizationDeptFilter');

        if (searchInput) {
            searchInput.addEventListener('input', applyFilters);
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', applyFilters);
        }

        if (deptFilter) {
            deptFilter.addEventListener('change', applyFilters);
        }
    });

})();
