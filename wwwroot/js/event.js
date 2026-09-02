"use strict";

$(document).ready(function () {

    let table;

    const trainingEventTableStateKey =
        "TrainingEvent.Index.DataTableState";

    let isMobile = window.matchMedia(
        "(max-width: 767.98px)"
    ).matches;

    
    // =========================================================
    // HELPERS
    // =========================================================

    function escapeHtml(value) {

        if (value === null || value === undefined) {
            return "";
        }

        return $("<div>")
            .text(value)
            .html();
    }


    function getValue(row, property) {

        if (!row) {
            return "";
        }

        return row[property] ??
            row[property.charAt(0).toUpperCase() + property.slice(1)] ??
            "";
    }


    function formatDate(value) {

        if (!value) {
            return "-";
        }

        const date = new Date(value);

        if (isNaN(date.getTime())) {
            return value;
        }

        const months = [
            "Jan", "Feb", "Mar", "Apr",
            "May", "Jun", "Jul", "Aug",
            "Sep", "Oct", "Nov", "Dec"
        ];

        return (
            String(date.getDate()).padStart(2, "0") +
            " " +
            months[date.getMonth()] +
            " " +
            date.getFullYear()
        );
    }


    function renderType(value) {

        if (!value) {
            return "-";
        }

        const type = String(value).toLowerCase();

        if (type === "i") {

            return `
                <span class="badge bg-primary event-type-badge">
                    Int
                </span>
            `;
        }

        if (type === "e") {

            return `
                <span class="badge bg-info text-dark event-type-badge">
                    Ext
                </span>
            `;
        }

        return `
            <span class="badge bg-secondary event-type-badge">
                ${escapeHtml(value)}
            </span>
        `;
    }


    function renderStatus(value) {

        if (!value) {
            return "-";
        }

        const status = String(value).toLowerCase();
        
        let cssClass = "bg-secondary";

        switch (status) {

            case "approved":
                cssClass = "bg-success";
                break;

            case "pending":
                cssClass = "bg-warning text-dark";
                break;

            case "canceled":
                cssClass = "bg-danger";
                break;

            case "rejected":
                cssClass = "bg-danger";
                break;

            case "draft":
            default:
                cssClass = "bg-secondary";
                break;
        }

        return `
            <span class="badge ${cssClass} event-status-badge">
                ${escapeHtml(value)}
            </span>
        `;
    }


    // =========================================================
    // CUSTOM MOBILE DETAIL
    // =========================================================

    function formatMobileDetail(row) {

        const department =
            getValue(row, "deptName");

        const trainer =
            getValue(row, "trainerName");

        const eventType =
            getValue(row, "eventType");

        const category =
            getValue(row, "categoryName");

        const quota =
            getValue(row, "participantQuota");

        const sessionCount =
            getValue(row, "sessionCount");

        const durationHours =
            getValue(row, "durationHours");


        return `
            <div class="event-mobile-detail">

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Department
                    </div>
                    <div class="event-detail-value">
                        ${escapeHtml(department || "-")}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Trainer
                    </div>
                    <div class="event-detail-value">
                        ${escapeHtml(trainer || "-")}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Type
                    </div>
                    <div class="event-detail-value">
                        ${renderType(eventType)}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Category
                    </div>
                    <div class="event-detail-value">
                        ${escapeHtml(category || "-")}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Quota
                    </div>
                    <div class="event-detail-value">
                        ${escapeHtml(quota || "-")}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Session #
                    </div>
                    <div class="event-detail-value">
                        ${escapeHtml(sessionCount || "-")}
                    </div>
                </div>

                <div class="event-detail-item">
                    <div class="event-detail-label">
                        Duration
                    </div>
                    <div class="event-detail-value">
                        ${durationHours
                ? escapeHtml(durationHours) + " hrs"
                : "-"
            }
                    </div>
                </div>

            </div>
        `;
    }


    // =========================================================
    // DATATABLE
    // =========================================================

    table = $("#dTable").DataTable({

        processing: true,

        serverSide: true,

        autoWidth: false,

        responsive: false,

        stateSave: true,

        stateSaveCallback: function (settings, data) {

            sessionStorage.setItem(
                trainingEventTableStateKey,
                JSON.stringify(data)
            );

            console.log(
                "[TrainingEvent] DataTable state saved:",
                data
            );
        },

        stateLoadCallback: function () {

            const json =
                sessionStorage.getItem(
                    trainingEventTableStateKey
                );

            if (!json) {
                return null;
            }

            try {

                const state =
                    JSON.parse(json);

                console.log(
                    "[TrainingEvent] DataTable state restored:",
                    state
                );

                return state;

            }
            catch (error) {

                console.error(
                    "[TrainingEvent] Invalid DataTable state:",
                    error
                );

                sessionStorage.removeItem(
                    trainingEventTableStateKey
                );

                return null;
            }
        },

        pageLength: 10,

        lengthMenu: [
            [10, 25, 50, 100],
            [10, 25, 50, 100]
        ],


        order: [
            [7, "asc"]
        ],


        ajax: {

            url: urls.getEvents,

            type: "GET",

            beforeSend: function (xhr, settings) {

                console.log(
                    "[TrainingEvent] FINAL AJAX URL:",
                    settings.url
                );
            },

            data: function (d) {

                d.coCode =
                    $("#companyFilter").val() || null;

                d.abrv =
                    $("#departmentFilter").val() || null;

                d.status =
                    $("#statusFilter").val() || null;

                d.searchTerm =
                    d.search &&
                        d.search.value
                        ? d.search.value
                        : null;


                if (
                    d.order &&
                    d.order.length > 0 &&
                    d.columns
                ) {

                    const order =
                        d.order[0];

                    const column =
                        d.columns[order.column];

                    d.orderColumn =
                        column.name || "EventStartDate";

                    d.orderDirection =
                        (
                            order.dir || "asc"
                        ).toUpperCase();
                }
                else {

                    d.orderColumn =
                        "EventStartDate";

                    d.orderDirection =
                        "ASC";
                }


                // Remove DataTables' unnecessary parameters
                delete d.columns;
                delete d.order;
                delete d.search;
            },

            dataSrc: function (json) {

                return json && json.data
                    ? json.data
                    : [];
            },

            error: function (xhr) {

                console.error(
                    "GetEvents error:",
                    xhr.responseText
                );
            }
        },


        // =====================================================
        // COLUMNS
        // =====================================================

        columns: [

            // -------------------------------------------------
            // CONTROL
            // -------------------------------------------------

            {
                data: null,

                name: "",

                title: "",

                orderable: false,

                searchable: false,

                className:
                    "event-control",

                width: "32px",

                defaultContent: ""
            },


            // -------------------------------------------------
            // EVENT CODE
            // -------------------------------------------------

            {
                data: "eventCode",

                name: "EventCode",

                title: "Event Code",

                className:
                    "event-code-column"
            },


            // -------------------------------------------------
            // TRAINING
            // -------------------------------------------------

            {
                data: "trainingTitle",

                name: "TrainingTitle",

                title: "Training",

                className:
                    "training-column",

                render: function (data) {

                    return `
                        <div class="event-training-title">
                            ${escapeHtml(data || "-")}
                        </div>
                    `;
                }
            },


            // -------------------------------------------------
            // DEPARTMENT
            // -------------------------------------------------

            {
                data: "deptName",

                name: "DeptName",

                title: "Department",

                className:
                    "department-column"
            },


            // -------------------------------------------------
            // TRAINER
            // -------------------------------------------------

            {
                data: "trainerName",

                name: "TrainerName",

                title: "Trainer",

                className:
                    "trainer-column"
            },


            // -------------------------------------------------
            // TYPE
            // -------------------------------------------------

            {
                data: "eventType",

                name: "EventType",

                title: "Type",

                className:
                    "type-column text-center",

                render: function (data) {

                    return renderType(data);
                }
            },


            // -------------------------------------------------
            // CATEGORY
            // -------------------------------------------------

            {
                data: "categoryName",

                name: "CategoryName",

                title: "Category",

                className:
                    "category-column"
            },


            // -------------------------------------------------
            // START DATE
            // -------------------------------------------------

            {
                data: "eventStartDate",

                name: "EventStartDate",

                title: "Date",

                className:
                    "start-date-column",

                render: function (data) {

                    return formatDate(data);
                }
            },


            // -------------------------------------------------
            // QUOTA
            // -------------------------------------------------

            {
                data: "participantQuota",

                name: "ParticipantQuota",

                title: "#Part",

                className:
                    "quota-column text-center"
            },


            // -------------------------------------------------
            // SESSION
            // -------------------------------------------------

            {
                data: "sessionCount",

                name: "SessionCount",

                title: "Sess",

                className:
                    "session-column text-center"
            },


            // -------------------------------------------------
            // DURATION
            // -------------------------------------------------

            {
                data: "durationHours",

                name: "DurationHours",

                title: "Duration",

                className:
                    "duration-column text-center",

                render: function (data) {

                    if (
                        data === null ||
                        data === undefined ||
                        data === ""
                    ) {
                        return "-";
                    }

                    return escapeHtml(data) + " hrs";
                }
            },


            // -------------------------------------------------
            // STATUS
            // -------------------------------------------------

            {
                data: "status",

                name: "Status",

                title: "Status",

                className:
                    "status-column text-center text-nowrap",

                width: "150px",

                render: function (data) {

                    return renderStatus(data);
                }
            },

            // -------------------------------------------------
            // Action
            // -------------------------------------------------

            {
                data: null,

                //name: "Action",

                title: "Action",

                orderable: false,

                searchable: false,

                className: "action-column text-center",

                width: "55px",

                render: function (data, type, row) {

                    if (!row.canShow) {
                        return '';
                    }

                    const url =
                        `${urls.detail}?id=${row.trainingEventId}`;

                    return `
                                <a href="${url}"
                                   class="btn btn-sm btn-primary"
                                   title="Detail">
                                    <i class="fas fa-eye"></i>
                                </a>
                            `;
                }
            }
        ]
    });


    // =========================================================
    // CUSTOM MOBILE MODE
    // =========================================================

    function applyMobileMode() {

        const mobile =
            window.matchMedia(
                "(max-width: 767.98px)"
            ).matches;

        if (mobile === isMobile) {
            return;
        }

        isMobile = mobile;

        table.page(0).draw("page");
    }


    // =========================================================
    // ROW CREATED
    // =========================================================

    table.on(
        "draw",
        function () {

            $("#dTable tbody tr").each(
                function () {

                    const row =
                        table.row(this);

                    if (!row.data()) {
                        return;
                    }

                    /*
                     * Remove old custom detail.
                     */

                    $(this)
                        .removeClass(
                            "event-mobile-open"
                        );
                }
            );


            /*
             * On mobile, show control icon.
             */

            if (
                window.matchMedia(
                    "(max-width: 767.98px)"
                ).matches
            ) {

                $("#dTable tbody tr")
                    .each(
                        function () {

                            const row =
                                table.row(this);

                            if (!row.data()) {
                                return;
                            }

                            const control =
                                $(this)
                                    .find(
                                        "td.event-control"
                                    );

                            control.html(
                                '<span class="event-expand-icon">›</span>'
                            );
                        }
                    );
            }
            else {

                $("#dTable tbody tr")
                    .find(
                        "td.event-control"
                    )
                    .empty();
            }
        }
    );


    // =========================================================
    // CUSTOM MOBILE EXPAND
    // =========================================================

    $("#dTable tbody").on(
        "click",
        "td.event-control",
        function () {

            if (
                !window.matchMedia(
                    "(max-width: 767.98px)"
                ).matches
            ) {
                return;
            }

            const tr =
                $(this).closest("tr");

            const row =
                table.row(tr);

            if (!row.data()) {
                return;
            }


            /*
             * Already open
             */

            if (row.child.isShown()) {

                row.child.hide();

                tr.removeClass(
                    "event-mobile-open"
                );

                $(this).html(
                    '<span class="event-expand-icon">›</span>'
                );

                return;
            }


            /*
             * Open
             */

            row.child(
                formatMobileDetail(
                    row.data()
                ),
                "event-mobile-child"
            ).show();

            tr.addClass(
                "event-mobile-open"
            );

            $(this).html(
                '<span class="event-expand-icon">⌄</span>'
            );
        }
    );


    // =========================================================
    // COMPANY
    // =========================================================

    function loadCompanies() {

        const ddl = $("#companyFilter");

        ddl.empty();

        $.ajax({
            url: urls.getCompanies,
            type: "GET",
            dataType: "json",

            success: function (response) {

                console.log("GetCompanies RAW:", response);

                let companies = [];

                if (Array.isArray(response)) {
                    companies = response;
                }
                else if (response?.data && Array.isArray(response.data)) {
                    companies = response.data;
                }
                else if (response?.Data && Array.isArray(response.Data)) {
                    companies = response.Data;
                }


                console.log(
                    "GetCompanies NORMALIZED:",
                    companies
                );


                if (companies.length === 0) {

                    ddl.append(
                        $("<option>", {
                            value: "",
                            text: "No Company"
                        })
                    );

                    loadDepartments();

                    return;
                }


                function getCompanyValue(item) {

                    return (
                        item.coCode ??
                        item.CoCode ??
                        item.value ??
                        item.Value ??
                        item.code ??
                        item.Code ??
                        ""
                    );
                }


                function getCompanyText(item) {

                    return (
                        item.companyName ??
                        item.CompanyName ??
                        item.coName ??
                        item.CoName ??
                        item.text ??
                        item.Text ??
                        item.name ??
                        item.Name ??
                        getCompanyValue(item)
                    );
                }


                // ================================================
                // ONLY ONE COMPANY
                // ================================================

                if (companies.length === 1) {

                    const item = companies[0];

                    const value =
                        getCompanyValue(item);

                    const text =
                        getCompanyText(item);


                    ddl.append(
                        $("<option>", {
                            value: value,
                            text: text
                        })
                    );

                    ddl.val(value);


                    console.log(
                        "Company selected:",
                        value,
                        text
                    );

                    // ================================================
                    // RESTORE SAVED COMPANY
                    // ================================================

                    restoreCompanyFilterState();


                    // ================================================
                    // LOAD DEPARTMENT
                    // ================================================

                    loadDepartments();

                    return;
                }


                // ================================================
                // MULTIPLE COMPANIES
                // ================================================

                ddl.append(
                    $("<option>", {
                        value: "",
                        text: "All Company"
                    })
                );


                $.each(
                    companies,
                    function (_, item) {

                        const value =
                            getCompanyValue(item);

                        const text =
                            getCompanyText(item);


                        ddl.append(
                            $("<option>", {
                                value: value,
                                text: text
                            })
                        );
                    }
                );

                // ================================================
                // RESTORE SAVED COMPANY
                // ================================================

                restoreCompanyFilterState();


                // ================================================
                // LOAD DEPARTMENT
                // ================================================

                loadDepartments();
            },


            error: function (xhr) {

                console.error(
                    "GetCompanies FAILED:",
                    xhr.status,
                    xhr.responseText
                );


                ddl.empty();

                ddl.append(
                    $("<option>", {
                        value: "",
                        text: "Failed to load company"
                    })
                );


                loadDepartments();
            }
        });
    }


    // =========================================================
    // DEPARTMENT
    // =========================================================

    function loadDepartments() {

        const ddl = $("#departmentFilter");

        ddl.empty();

        $.ajax({
            url: urls.getDepartments,
            type: "GET",
            dataType: "json",

            success: function (response) {

                console.log(
                    "GetDepartments RAW:",
                    response
                );


                let departments = [];

                if (Array.isArray(response)) {
                    departments = response;
                }
                else if (
                    response?.data &&
                    Array.isArray(response.data)
                ) {
                    departments = response.data;
                }
                else if (
                    response?.Data &&
                    Array.isArray(response.Data)
                ) {
                    departments = response.Data;
                }


                console.log(
                    "GetDepartments NORMALIZED:",
                    departments
                );


                // ================================================
                // NO DATA
                // ================================================

                if (departments.length === 0) {

                    ddl.append(
                        $("<option>", {
                            value: "",
                            text: "No Department"
                        })
                    );

                    restoreTrainingEventFilterState();

                    table.ajax.reload(
                        null,
                        false
                    );

                    return;
                }


                // ================================================
                // GET VALUE
                // ================================================

                function getDepartmentValue(item) {

                    return (
                        item.abrv ??
                        item.ABRV ??
                        item.value ??
                        item.Value ??
                        item.code ??
                        item.Code ??
                        item.deptCode ??
                        item.DeptCode ??
                        ""
                    );
                }


                // ================================================
                // GET TEXT
                // ================================================

                function getDepartmentText(item) {

                    return (
                        item.deptName ??
                        item.DeptName ??
                        item.departmentName ??
                        item.DepartmentName ??
                        item.text ??
                        item.Text ??
                        item.name ??
                        item.Name ??
                        getDepartmentValue(item)
                    );
                }


                // ================================================
                // ONLY ONE DEPARTMENT
                // ================================================

                if (departments.length === 1) {

                    const item =
                        departments[0];


                    const value =
                        getDepartmentValue(item);


                    const text =
                        getDepartmentText(item);


                    ddl.append(
                        $("<option>", {
                            value: value,
                            text: text
                        })
                    );


                    ddl.val(value);


                    console.log(
                        "Department selected:",
                        value,
                        text
                    );

                    // ================================================
                    // RESTORE SAVED FILTER
                    // ================================================

                    restoreTrainingEventFilterState();


                    // ================================================
                    // LOAD TABLE
                    // ================================================

                    table.ajax.reload(
                        null,
                        false
                    );


                    return;
                }


                // ================================================
                // MULTIPLE DEPARTMENTS
                // ================================================

                ddl.append(
                    $("<option>", {
                        value: "",
                        text: "All Department"
                    })
                );


                $.each(
                    departments,
                    function (_, item) {

                        const value =
                            getDepartmentValue(item);


                        const text =
                            getDepartmentText(item);


                        ddl.append(
                            $("<option>", {
                                value: value,
                                text: text
                            })
                        );
                    }
                );


                // ================================================
                // RESTORE SAVED FILTER
                // ================================================

                restoreTrainingEventFilterState();


                // ================================================
                // LOAD TABLE
                // ================================================

                table.ajax.reload(
                    null,
                    false
                );
            },


            error: function (xhr) {

                console.error(
                    "GetDepartments FAILED:",
                    xhr.status,
                    xhr.responseText
                );


                ddl.empty();

                ddl.append(
                    $("<option>", {
                        value: "",
                        text: "Failed to load department"
                    })
                );

                restoreTrainingEventFilterState();

                table.ajax.reload(
                    null,
                    false
                );
            }
        });
    }


    // =========================================================
    // FILTER
    // =========================================================

    $("#companyFilter").on(
        "change",
        function () {

            saveTrainingEventFilterState();

            loadDepartments();
        }
    );


    $("#departmentFilter").on(
        "change",
        function () {

            saveTrainingEventFilterState();

            table.ajax.reload(
                null,
                true
            );
        }
    );


    $("#statusFilter").on(
        "change",
        function () {

            saveTrainingEventFilterState();

            table.ajax.reload(
                null,
                true
            );
        }
    );


    $("#clearFilters").on(
        "click",
        function () {

            sessionStorage.removeItem(
                "TrainingEvent.Index.Filters"
            );

            sessionStorage.removeItem(
                "TrainingEvent.Index.DataTableState"
            );

            $("#statusFilter").val("");
            $("#departmentFilter").val("");
            $("#companyFilter").val("");

            loadCompanies();
        }
    );


    // =========================================================
    // RESIZE
    // =========================================================

    $(window).on(
        "resize orientationchange",
        function () {

            clearTimeout(
                window.eventResizeTimer
            );

            window.eventResizeTimer =
                setTimeout(
                    function () {

                        const currentMobile =
                            window.matchMedia(
                                "(max-width: 767.98px)"
                            ).matches;

                        if (
                            currentMobile !==
                            isMobile
                        ) {

                            isMobile =
                                currentMobile;

                            table.ajax.reload(
                                null,
                                false
                            );
                        }

                        table.columns.adjust();

                    },
                    200
                );
        }
    );


    // =========================================================
    // INITIAL LOAD
    // =========================================================

    loadCompanies();

    // =========================================================
    // Save Filter state
    // =========================================================

    function saveTrainingEventFilterState() {

        const state = {
            company: $('#companyFilter').val(),
            department: $('#departmentFilter').val(),
            status: $('#statusFilter').val()
        };

        sessionStorage.setItem(
            'TrainingEvent.Index.Filters',
            JSON.stringify(state)
        );

        console.log(
            '[TrainingEvent] Filter state saved:',
            state
        );
    }

    //============================================================
    // Get Filter state
    //============================================================

    function getTrainingEventFilterState() {

        const json = sessionStorage.getItem(
            "TrainingEvent.Index.Filters"
        );

        if (!json) {
            return null;
        }

        try {

            return JSON.parse(json);

        } catch (error) {

            console.error(
                "[TrainingEvent] Invalid filter state:",
                error
            );

            sessionStorage.removeItem(
                "TrainingEvent.Index.Filters"
            );

            return null;
        }
    }

    function restoreCompanyFilterState() {

        const state =
            getTrainingEventFilterState();

        if (!state || state.company === undefined) {
            return false;
        }

        const $company =
            $("#companyFilter");

        const companyExists =
            $company.find(
                "option[value='" +
                state.company +
                "']"
            ).length > 0;

        if (!companyExists) {
            return false;
        }

        $company.val(state.company);

        console.log(
            "[TrainingEvent] Company restored:",
            state.company
        );

        return true;
    }

    //=========================================================
    // Load Filter state
    //=========================================================

    function restoreTrainingEventFilterState() {

        const state =
            getTrainingEventFilterState();

        if (!state) {

            console.log(
                "[TrainingEvent] No saved filter state."
            );

            return false;
        }

        console.log(
            "[TrainingEvent] Restoring filter state:",
            state
        );


        // ================================================
        // COMPANY
        // ================================================

        if (state.company !== undefined) {

            const companyExists =
                $("#companyFilter option[value='" +
                    state.company +
                    "']").length > 0;

            if (companyExists) {

                $("#companyFilter").val(
                    state.company
                );

                console.log(
                    "[TrainingEvent] Company restored:",
                    state.company
                );
            }
        }


        // ================================================
        // DEPARTMENT
        // ================================================

        if (state.department !== undefined) {

            const departmentExists =
                $("#departmentFilter option[value='" +
                    state.department +
                    "']").length > 0;

            if (departmentExists) {

                $("#departmentFilter").val(
                    state.department
                );

                console.log(
                    "[TrainingEvent] Department restored:",
                    state.department
                );
            }
            else {

                console.warn(
                    "[TrainingEvent] Department option not found:",
                    state.department
                );
            }
        }


        // ================================================
        // STATUS
        // ================================================

        if (state.status !== undefined) {

            $("#statusFilter").val(
                state.status
            );

            console.log(
                "[TrainingEvent] Status restored:",
                state.status
            );
        }

        return true;
    }

});