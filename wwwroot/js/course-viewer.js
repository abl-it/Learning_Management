/* Master Course - View Only modal (#courseViewerModal)
   Partial: Views/Shared/_CourseViewerModal.cshtml
   Data   : POST Master/GetCourses (server-side DataTables, sama dengan halaman Master Course) */
(function ($) {
    'use strict';

    var modalEl = document.getElementById('courseViewerModal');
    if (!modalEl || !$.fn.DataTable) return;

    var url = modalEl.getAttribute('data-url');
    var table = null;

    function esc(s) {
        return $('<div>').text(s == null ? '' : s).html();
    }

    function initTable() {
        // DataTable dibuat saat modal pertama kali tampil agar lebar kolom benar
        if (table) {
            table.columns.adjust();
            return;
        }

        table = $('#cvTable').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            autoWidth: false,
            pageLength: 10,
            lengthMenu: [[5, 10, 25, 50, 100], [5, 10, 25, 50, 100]],
            ajax: {
                url: url,
                type: 'POST',
                data: function (d) {
                    d.categoryFilter = $('#cvCategoryFilter').val();
                    d.statusFilter = $('#cvStatusFilter').val();
                }
            },
            columns: [
                { data: 'courseId', name: 'Id', responsivePriority: 1000, className: 'text-center fw-medium' },
                { data: 'courseCode', name: 'Code', responsivePriority: 1, render: function (d, t) { return t === 'display' ? esc(d) : d; } },
                { data: 'courseName', name: 'Name', responsivePriority: 2, render: function (d, t) { return t === 'display' ? esc(d) : d; } },
                {
                    data: 'description', name: 'Description', responsivePriority: 4,
                    render: function (data, type) {
                        if (type !== 'display') return data;
                        var max = 90;
                        var txt = data || '';
                        var shown = txt.length > max ? txt.substring(0, max) + '...' : txt;
                        return '<span title="' + esc(txt) + '">' + esc(shown) + '</span>';
                    }
                },
                { data: 'categoryCode', name: 'Category', responsivePriority: 4, className: 'text-center' },
                { data: 'durationHours', name: 'Duration', responsivePriority: 4, className: 'text-center' },
                { data: 'trainingProvider', name: 'Provider', responsivePriority: 4, className: 'text-center' },
                {
                    data: 'isActive', name: 'IsActive', responsivePriority: 1, className: 'text-center',
                    render: function (data) {
                        return data == 1
                            ? '<span class="badge bg-success">Active</span>'
                            : '<span class="badge bg-secondary">Inactive</span>';
                    }
                }
            ],
            order: [[0, 'desc']],
            language: {
                processing: '<i class="fas fa-spinner fa-spin me-2"></i>Loading...',
                search: 'Search:',
                lengthMenu: 'Show _MENU_',
                info: '_START_-_END_ of _TOTAL_',
                infoEmpty: 'No courses found',
                infoFiltered: '(filtered from _MAX_)',
                zeroRecords: 'No matching courses found',
                paginate: {
                    first: '<i class="fas fa-angle-double-left"></i>',
                    previous: '<i class="fas fa-angle-left"></i>',
                    next: '<i class="fas fa-angle-right"></i>',
                    last: '<i class="fas fa-angle-double-right"></i>'
                }
            },
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                 '<"row"<"col-sm-12"tr>>' +
                 '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>'
        });
    }

    modalEl.addEventListener('shown.bs.modal', initTable);

    $('#cvCategoryFilter, #cvStatusFilter').on('change', function () {
        if (table) table.ajax.reload();
    });

    $('#cvClearFilters').on('click', function () {
        $('#cvCategoryFilter').val('All');
        $('#cvStatusFilter').val('');
        if (table) table.ajax.reload();
    });
})(jQuery);
