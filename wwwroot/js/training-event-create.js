(function ($) {

    'use strict';

    console.log('===== training-event-create.js LOADED =====');

    

    const TrainingEventCreate = {

        urls: {
            getCourses: '',
            getCompanies: '',
            getDepartments: '',
            searchEmployees: '',
            save: '',
            index: ''
        },

        urls: window.trainingEventCreateUrls || {},

        state: {
            selectedCourse: null,
            selectedTrainer: null,
            participants: []
        },


        init: function () {

            console.log('[CREATE] init START');

            this.urls =
                window.trainingEventCreateUrls || {};

            console.log(
                '[CREATE] URLs:',
                this.urls
            );

            this.bindEvents();

            this.loadCompanies();

            initCourseTypeahead();

            initTrainerTypeahead();

            initParticipant();

            console.log('[CREATE] init END');
        },


        

        /*
         * ========================================================
         * DEPARTMENT
         * ========================================================
         */

        loadDepartments: function () {

            const self = this;
            const $department = $('#ABRV');

            if (!$department.length) {
                console.error(
                    'TrainingEventCreate: #ABRV tidak ditemukan.'
                );
                return;
            }

            const selectedCoCode = $('#CoCode').val() || '';

            const url = self.urls.getDepartments +
                (selectedCoCode
                    ? (self.urls.getDepartments.indexOf('?') === -1 ? '?' : '&') +
                      'coCode=' + encodeURIComponent(selectedCoCode)
                    : '');

            console.log(
                'Loading departments from:',
                url
            );

            if (!url) {

                console.error(
                    'TrainingEventCreate: getDepartments URL tidak tersedia.'
                );

                return;
            }

            $department
                .prop('disabled', true)
                .empty()
                .append(
                    $('<option>')
                        .val('')
                        .text('Loading...')
                );

            $.ajax({
                url: url,
                type: 'GET',
                dataType: 'json',
                cache: false,

                success: function (response) {

                    console.log(
                        '[CREATE] Department SUCCESS:',
                        response
                    );

                    if (!Array.isArray(response) || response.length === 0) {

                        $department
                            .empty()
                            .append(
                                $('<option>')
                                    .val('')
                                    .text('No Department Available')
                            )
                            .prop('disabled', true);

                        return;
                    }

                    // =====================================================
                    // HANYA 1 DEPARTMENT
                    // =====================================================
                    if (response.length === 1) {

                        const department = response[0];

                        $department
                            .empty()
                            .append(
                                $('<option>')
                                    .val(department.value)
                                    .text(department.text)
                            )
                            .val(department.value)
                            .prop('disabled', true);

                        console.log(
                            '[CREATE] Department auto selected:',
                            department.value
                        );

                        return;
                    }

                    // =====================================================
                    // LEBIH DARI 1 DEPARTMENT
                    // =====================================================
                    $department
                        .empty()
                        .append(
                            $('<option>')
                                .val('')
                                .text('Select Department')
                        );

                    $.each(response, function (_, item) {

                        $department.append(
                            $('<option>')
                                .val(item.value)
                                .text(item.text)
                        );

                    });

                    $department.prop('disabled', false);
                },

                error: function (xhr, status, error) {

                    console.error(
                        'GetDepartments FAILED:',
                        xhr.status,
                        status,
                        error,
                        xhr.responseText
                    );

                    $department
                        .empty()
                        .append(
                            $('<option>')
                                .val('')
                                .text('Failed to load department')
                        )
                        .prop('disabled', false);
                }
            });
        },


        /*
         * ========================================================
         * COMPANY
         * ========================================================
         */

        loadCompanies: function () {

            console.log('[CREATE] loadCompanies START');

            const self = this;
            const $company = $('select#CoCode');

            console.log(
                '[CREATE] company element:',
                $company.length
            );

            console.log(
                '[CREATE] company URL:',
                self.urls.getCompanies
            );

            if (!$company.length) {

                console.error(
                    '[CREATE] select#CoCode tidak ditemukan.'
                );

                return;
            }

            const url = self.urls.getCompanies;

            if (!url) {

                console.error(
                    '[CREATE] getCompanies URL kosong.'
                );

                return;
            }

            $company
                .prop('disabled', true)
                .empty()
                .append(
                    $('<option>')
                        .val('')
                        .text('Loading...')
                );

            $.ajax({

                url: url,
                type: 'GET',
                dataType: 'json',
                cache: false,

                success: function (response) {

                    console.log('[CREATE] Company SUCCESS:', response);

                    if (!Array.isArray(response) || response.length === 0) {

                        $company
                            .empty()
                            .append(
                                $('<option>')
                                    .val('')
                                    .text('No Company Available')
                            )
                            .prop('disabled', true);

                        return;
                    }

                    // =====================================================
                    // HANYA 1 COMPANY
                    // =====================================================
                    if (response.length === 1) {

                        const company = response[0];

                        $company
                            .empty()
                            .append(
                                $('<option>')
                                    .val(company.value)
                                    .text(company.text)
                            )
                            .val(company.value)
                            .prop('disabled', true);

                        console.log(
                            '[CREATE] Company auto selected:',
                            company.value
                        );

                        // Setelah company terpilih, load department
                        self.loadDepartments();

                        return;
                    }

                    // =====================================================
                    // LEBIH DARI 1 COMPANY
                    // (tidak ada opsi kosong "Select Company" - harus
                    // pilih company yang nyata; default ke ABL kalau ada)
                    // =====================================================
                    $company.empty();

                    $.each(response, function (_, item) {

                        $company.append(
                            $('<option>')
                                .val(item.value)
                                .text(item.text)
                        );

                    });

                    $company.prop('disabled', false);

                    const hasAbl = response.some(function (item) {
                        return item.value === 'ABL';
                    });

                    if (hasAbl) {

                        $company.val('ABL');

                        console.log(
                            '[CREATE] Company defaulted to ABL'
                        );
                    }

                    // Load department for whichever company ended up
                    // selected (ABL default, or the browser's natural
                    // first-option selection if ABL isn't available).
                    self.loadDepartments();
                },

                error: function (xhr, status, error) {

                    console.error(
                        '[CREATE] Company ERROR:',
                        xhr.status,
                        status,
                        error,
                        xhr.responseText
                    );

                    $company
                        .empty()
                        .append(
                            $('<option>')
                                .val('')
                                .text('Failed to load company')
                        )
                        .prop(
                            'disabled',
                            false
                        );
                }
            });
        },

        /* 
        * ========================================================
        * BIND EVENTS
        * ========================================================
        */

        bindEvents: function () {

            const self = this;


            /*
             * Company
             */
            $(document).on(
                'change.trainingEventCreate',
                '#CoCode',
                function () {

                    self.loadDepartments();

                }
            );


            /*
             * Trainer Type
             */
            $(document).on(
                'change.trainingEventCreate',
                '#TrainerType',
                function () {

                    if (
                        typeof self.updateTrainerMode ===
                        'function'
                    ) {

                        self.updateTrainerMode();

                    }

                }
            );


            /*
             * Add Participant
             */
            $(document).on(
                'click.trainingEventCreate',
                '#btnAddParticipant',
                function (e) {

                    e.preventDefault();

                    if (
                        typeof self.openParticipantModal ===
                        'function'
                    ) {

                        self.openParticipantModal();

                    }

                }
            );


            /*
             * Save
             */
            $(document).on(
                'click.trainingEventCreate',
                '#btnSave',
                function (e) {

                    e.preventDefault();

                    if (
                        typeof self.submitForm ===
                        'function'
                    ) {

                        self.submitForm();

                    }

                }
            );


            /*
             * Cancel
             */
            $(document).on(
                'click.trainingEventCreate',
                '#btnCancel',
                function (e) {

                    e.preventDefault();

                    if (self.urls.index) {

                        window.location.href =
                            self.urls.index;

                    } else {

                        window.history.back();

                    }

                }
            );

        },

    };


   

    window.TrainingEventCreate =
        TrainingEventCreate;


    $(document).ready(

        function () {

            TrainingEventCreate.init();

            $('#btnSaveEvent').on('click', function (e) {

                e.preventDefault();

                console.log('[CREATE] SAVE BUTTON CLICKED');

                saveEvent();
            });
            
        }
    );

    function initCourseTypeahead() {

        const $course = $('#courseSearch');

        if (!$course.length) {
            console.warn('[CREATE] CourseSearch not found.');
            return;
        }

        if (typeof $course.typeahead !== 'function') {
            console.error(
                '[CREATE] bootstrap3-typeahead is not loaded.'
            );
            return;
        }

        $course.typeahead({
            minLength: 2,
            items: 10,
            autoSelect: false,

            source: function (query, process) {

                $.ajax({
                    url: window.trainingEventCreateUrls.getCourses,
                    type: 'GET',
                    dataType: 'json',
                    data: {
                        search: query
                    },
                    success: function (response) {

                        console.log(
                            '[CREATE] Courses:',
                            response
                        );

                        process(response);
                    },
                    error: function (xhr) {

                        console.error(
                            '[CREATE] GetCourses failed:',
                            xhr.status,
                            xhr.responseText
                        );

                        process([]);
                    }
                });
            },

            displayText: function (item) {

                return item.courseCode +
                    ' - ' +
                    item.courseName;
            },

            afterSelect: function (item) {

                console.log(
                    '[CREATE] Course selected:',
                    item
                );

                $('#CourseId').val(item.courseId);
                $('#CourseCode').val(item.courseCode);

                $('#TrainingTitle').val(
                    item.courseName
                );

                $('#CategoryId').val(
                    item.categoryId
                );

                $('#CategoryCode').val(
                    item.categoryCode
                );

                $('#CategoryName').val(
                    item.categoryName ||
                    item.categoryCode ||
                    ''
                );

                $('#HoursPerSession').val(
                    item.durationHours
                );
            }
        });
    }

    function initTrainerTypeahead() {

        const $trainer = $('#TrainerSearch');

        if (!$trainer.length) {
            console.warn(
                '[CREATE] TrainerSearch not found.'
            );
            return;
        }

        if (typeof $.fn.typeahead !== 'function') {

            console.error(
                '[CREATE] bootstrap3-typeahead is NOT loaded.'
            );

            return;
        }

        initializeTrainerMode();

        $('#TrainerType')
            .off('change.trainingTrainer')
            .on(
                'change.trainingTrainer',
                function () {

                    initializeTrainerMode();

                }
            );
    }

    function initializeTrainerMode() {

        const type =
            $('#TrainerType').val();

        const $trainer =
            $('#TrainerSearch');

        /*
         * Hapus instance typeahead sebelumnya.
         *
         * bootstrap3-typeahead menyimpan instance
         * pada data('typeahead').
         */
        if ($trainer.data('typeahead')) {

            $trainer
                .typeahead('destroy');

            $trainer.off(
                'keyup.trainingTrainer'
            );
        }

        /*
         * Reset trainer selection.
         */
        $('#TrainerEmployeeCode')
            .val('');

        $('#TrainerName')
            .val('');

        $trainer.val('');

        /*
         * =====================================================
         * INTERNAL
         * =====================================================
         */
        if (type === 'Internal') {

            $trainer
                .prop('readonly', false)
                .attr(
                    'placeholder',
                    'Search employee...'
                );

            initInternalTrainerTypeahead();

            return;
        }

        /*
         * =====================================================
         * EXTERNAL
         * =====================================================
         */

        $trainer
            .prop('readonly', false)
            .attr(
                'placeholder',
                'Enter trainer name...'
            );

        /*
         * External adalah FREE TEXT.
         *
         * Tidak ada AJAX.
         * Tidak ada typeahead.
         */
        $trainer
            .off('input.trainingTrainer')
            .on(
                'input.trainingTrainer',
                function () {

                    $('#TrainerEmployeeCode')
                        .val('');

                    $('#TrainerName')
                        .val($(this).val());
                }
            );
    }

    function initInternalTrainerTypeahead() {

        const $trainer =
            $('#TrainerSearch');

        const url =
            window.trainingEventCreateUrls
                .searchEmployees;

        if (!url) {

            console.error(
                '[CREATE] SearchEmployees URL tidak tersedia.'
            );

            return;
        }

        $trainer.typeahead({

            minLength: 2,

            items: 10,

            autoSelect: false,

            source: function (
                query,
                process
            ) {

                console.log(
                    '[CREATE] Searching employee:',
                    query
                );

                $.ajax({

                    url: url,

                    type: 'GET',

                    dataType: 'json',

                    data: {
                        term: query
                    }

                })
                    .done(function (response) {

                        console.log(
                            '[CREATE] Employee result:',
                            response
                        );

                        process(
                            Array.isArray(response)
                                ? response
                                : []
                        );
                    })
                    .fail(function (
                        xhr,
                        status,
                        error
                    ) {

                        console.error(
                            '[CREATE] SearchEmployees ERROR:',
                            {
                                statusCode: xhr.status,
                                status: status,
                                error: error,
                                response: xhr.responseText
                            }
                        );

                        process([]);
                    });
            },

            displayText: function (item) {

                return (
                    item.employeeCode +
                    ' - ' +
                    item.fullName
                );
            },

            afterSelect: function (item) {

                console.log(
                    '[CREATE] Internal Trainer selected:',
                    item
                );

                $('#TrainerEmployeeCode')
                    .val(item.employeeCode);

                $('#TrainerName')
                    .val(item.fullName);

                $trainer.val(
                    item.employeeCode +
                    ' - ' +
                    item.fullName
                );
            }
        });
    }

    function showWarning(message, title = 'Validation') {

        Swal.fire({
            icon: 'warning',
            title: title,
            text: message,
            confirmButtonText: 'OK'
        });

    }

    function initParticipant() {

        console.log('[CREATE] Initializing participants.');

        //initParticipantTypeahead();

        $('#btnAddParticipant')
            .off('click.trainingParticipant')
            .on('click.trainingParticipant', function () {

                const quota =
                    Number($('#Quota').val()) || 0;

                const participantCount =
                    TrainingEventCreate.state.participants.length;

                if (quota <= 0) {

                    showWarning(
                        'Please enter participant quota first.',
                        'Participant Quota'
                    );
                    return;
                }

                if (participantCount >= quota) {

                    showWarning(
                        'Participant quota has been reached. ' +
                        'Please increase the quota before adding another participant.',
                        'Participant Quota'
                    );

                    return;
                }

                openParticipantModal();

            });

        $('#btnConfirmParticipant')
            .off('click.trainingParticipant')
            .on('click.trainingParticipant', function () {

                addParticipant();

            });

        /*
         * Monitor perubahan quota.
         */
        $('#Quota')
            .off('change.trainingQuota')
            .on('change.trainingQuota', function () {

                validateQuotaChange();

            });     

    }

    function initParticipantTypeahead() {

        const $input = $('#ParticipantSearch');

        console.log(
            '[CREATE] ParticipantSearch element:',
            $input.length
        );

        if (!$input.length) {

            console.error(
                '[CREATE] #ParticipantSearch tidak ditemukan.'
            );

            return;
        }

        if (typeof $.fn.typeahead !== 'function') {

            console.error(
                '[CREATE] bootstrap3-typeahead tidak tersedia.'
            );

            return;
        }

        const url =
            window.trainingEventCreateUrls.searchEmployees;

        console.log(
            '[CREATE] Participant Search URL:',
            url
        );

        $input.typeahead({

            minLength: 2,

            items: 10,

            autoSelect: false,

            source: function (query, process) {

                console.log(
                    '[CREATE] Searching participant:',
                    query
                );

                $.ajax({
                    url: url,
                    type: 'GET',
                    dataType: 'json',
                    data: {
                        term: query
                    }
                })
                    .done(function (response) {

                        console.log(
                            '[CREATE] Participant search result:',
                            response
                        );

                        process(response || []);
                    })
                    .fail(function (xhr) {

                        console.error(
                            '[CREATE] Participant search failed:',
                            xhr.status,
                            xhr.responseText
                        );

                        process([]);
                    });
            },

            displayText: function (item) {

                return item.employeeCode +
                    ' - ' +
                    item.fullName;
            },

            afterSelect: function (item) {

                console.log(
                    '[CREATE] Participant selected:',
                    item
                );

                $('#ParticipantEmployeeCode')
                    .val(item.employeeCode || '');

                $('#ParticipantName')
                    .val(item.fullName || '');

                $('#ParticipantDepartment')
                    .val(item.abrv || '');

                $('#ParticipantDepartmentName')
                    .val(item.deptName || '');

                $('#participantSelectedCode')
                    .text(item.employeeCode || '');

                $('#participantSelectedName')
                    .text(item.fullName || '');

                $('#participantSelectedDepartment')
                    .text(item.deptName || '-');

                $('#participantSelectedInfo')
                    .removeClass('d-none');

                $('#btnConfirmParticipant')
                    .prop('disabled', false);

                console.log(
                    '[CREATE] Participant department:',
                    item.abrv,
                    item.deptName
                );
            }
        });
    }

    function validateQuotaChange() {

        const $quota =
            $('#Quota');

        const quota =
            Number($quota.val()) || 0;

        const participantCount =
            TrainingEventCreate.state.participants.length;

        if (quota <= 0) {

            return;
        }

        if (quota < participantCount) {

            showWarning(
                'Participant quota cannot be less than ' +
                'the number of participants already added.',
                'Participant Quota'
            );

            /*
             * Kembalikan ke quota minimum
             * yang masih memenuhi participant existing.
             */
            $quota.val(participantCount);

            return;
        }

        updateAddParticipantButton();
    }

    function updateAddParticipantButton() {

        const $button =
            $('#btnAddParticipant');

        if (!$button.length) {
            return;
        }

        const quota =
            Number($('#Quota').val()) || 0;

        const participantCount =
            TrainingEventCreate.state.participants.length;

        const reached =
            quota <= 0 ||
            participantCount >= quota;

        $button.prop(
            'disabled',
            reached
        );

        if (quota <= 0) {

            $button.attr(
                'title',
                'Please enter participant quota first.'
            );

        } else if (participantCount >= quota) {

            $button.attr(
                'title',
                'Participant quota has been reached.'
            );

        } else {

            $button.attr(
                'title',
                'Add participant'
            );

        }
    }

    function openParticipantModal() {

        resetParticipantModal();

        const modalElement =
            document.getElementById(
                'participantModal'
            );

        const modal =
            bootstrap.Modal.getOrCreateInstance(
                modalElement
            );

        modal.show();

        setTimeout(function () {

            initParticipantTypeahead();

            $('#ParticipantSearch')
                .trigger('focus');

        }, 300);
    }

    function resetParticipantModal() {

        $('#ParticipantSearch').val('');

        $('#ParticipantEmployeeCode').val('');

        $('#ParticipantName').val('');

        $('#ParticipantDepartment').val('');

        $('#participantSelectedCode').text('');

        $('#participantSelectedName').text('');

        $('#participantSelectedDepartment')
            .text('');

        $('#participantSelectedInfo')
            .addClass('d-none');

        $('#btnConfirmParticipant')
            .prop('disabled', true);
    }

    function addParticipant() {

        const participants =
            TrainingEventCreate.state.participants;

        const quota =
            Number($('#Quota').val()) || 0;

        /*
         * =====================================================
         * VALIDATE QUOTA
         * =====================================================
         */

        if (quota <= 0) {

            showWarning(
                'Please enter participant quota first.',
                'Participant Quota'
            );

            return;
        }

        if (participants.length >= quota) {

            showWarning(
                'Participant quota has been reached. ' +
                'Please increase the quota before adding another participant.',
                'Participant Quota'
            );

            updateAddParticipantButton();

            return;
        }

        /*
         * =====================================================
         * GET SELECTED EMPLOYEE
         * =====================================================
         */

        const employeeCode =
            $('#ParticipantEmployeeCode')
                .val()
                ?.trim();

        const fullName =
            $('#ParticipantName')
                .val()
                ?.trim();

        const departmentCode =
            $('#ParticipantDepartment')
                .val()
                ?.trim();

        const departmentName =
            $('#ParticipantDepartmentName')
                .val()
                ?.trim();

        if (!employeeCode) {

            showWarning(
                'Please select an employee.',
                'Employee'
            );

            return;
        }

        /*
         * =====================================================
         * DUPLICATE CHECK
         * =====================================================
         */

        const exists =
            participants.some(function (item) {

                return (
                    item.employeeCode?.toLowerCase() ===
                    employeeCode.toLowerCase()
                );

            });

        if (exists) {

            showWarning(
                'This employee has already been added.',
                'Employee'
            );

            return;
        }

        /*
         * =====================================================
         * ADD PARTICIPANT
         * =====================================================
         */

        const participant = {

            employeeCode: employeeCode,

            fullName: fullName,

            departmentCode: departmentCode,

            departmentName: departmentName

        };

        participants.push(participant);

        console.log(
            '[CREATE] Participant added:',
            participant
        );

        console.log(
            '[CREATE] Participants:',
            participants
        );

        renderParticipants();

        /*
         * Update Add button after participant added.
         */
        updateAddParticipantButton();

        /*
         * Close modal.
         */
        const modalElement =
            document.getElementById(
                'participantModal'
            );

        const modal =
            bootstrap.Modal.getInstance(
                modalElement
            );

        if (modal) {

            modal.hide();

        }

        resetParticipantModal();

    }

    function renderParticipants() {

        const participants =
            TrainingEventCreate.state.participants;

        console.log(
            '[CREATE] Rendering participants:',
            participants
        );

        const $tbody =
            $('#participantTableBody');

        const $empty =
            $('#participantEmpty');

        if (!$tbody.length) {

            console.error(
                '[CREATE] #participantTableBody tidak ditemukan di HTML.'
            );

            return;
        }

        $tbody.empty();

        /*
         * =====================================================
         * EMPTY
         * =====================================================
         */

        if (participants.length === 0) {

            if ($empty.length) {
                $empty.removeClass('d-none');
            }

            updateAddParticipantButton();

            return;
        }

        if ($empty.length) {
            $empty.addClass('d-none');
        }


        /*
         * =====================================================
         * RENDER PARTICIPANTS
         * =====================================================
         */

        $.each(
            participants,
            function (index, participant) {

                const $row = $('<tr>');


                /*
                 * #
                 */
                $('<td>')
                    .addClass('text-center')
                    .text(index + 1)
                    .appendTo($row);


                /*
                 * Employee Code
                 */
                $('<td>')
                    .text(
                        participant.employeeCode || '-'
                    )
                    .appendTo($row);


                /*
                 * Name
                 */
                $('<td>')
                    .text(
                        participant.fullName ||
                        participant.name ||
                        '-'
                    )
                    .appendTo($row);


                /*
                 * ABRV
                 *
                 * departmentCode = ABRV
                 */
                $('<td>')
                    .text(
                        participant.departmentCode || '-'
                    )
                    .appendTo($row);


                /*
                 * Department
                 *
                 * departmentName = Department Name
                 */
                $('<td>')
                    .text(
                        participant.departmentName || '-'
                    )
                    .appendTo($row);


                /*
                 * Remove
                 */
                const $remove =
                    $('<button>', {

                        type: 'button',

                        class:
                            'btn btn-outline-danger btn-sm',

                        title:
                            'Remove participant'

                    })
                        .html(
                            '<i class="bi bi-trash"></i>'
                        )
                        .on(
                            'click',
                            function () {

                                removeParticipant(index);

                            }
                        );


                $('<td>')
                    .addClass('text-center')
                    .append($remove)
                    .appendTo($row);


                $tbody.append($row);

            }
        );


        /*
         * =====================================================
         * UPDATE ADD BUTTON
         * =====================================================
         */

        updateAddParticipantButton();


        console.log(
            '[CREATE] Participant table rendered:',
            participants.length
        );
    }

    function removeParticipant(index) {

        const participants =
            TrainingEventCreate.state.participants;

        if (
            index < 0 ||
            index >= participants.length
        ) {

            return;
        }

        participants.splice(
            index,
            1
        );

        renderParticipants();

        //updateAddParticipantButton();

    }

    function buildCreateRequest() {

        const trainerType = $('#TrainerType').val();
        const $department = $('#ABRV');

        const abrv = $department.val();

        const deptName =
            $department.find('option:selected').text().trim();

        let eventType = 'I';
        let trainerId = null;
        let trainerName = null;

        if (trainerType === 'Internal') {

            eventType = 'I';

            trainerId =
                $('#TrainerEmployeeCode').val()?.trim() || null;

            trainerName =
                $('#TrainerName').val()?.trim() || null;

        } else {

            eventType = 'E';

            trainerId = null;

            trainerName =
                $('#TrainerSearch').val()?.trim() || null;
        }

        return {
            courseId: Number($('#CourseId').val()),

            trainingCategoryId:
                Number($('#CategoryId').val()),

            trainingTitle:
                $('#TrainingTitle').val()?.trim() || '',

            trainingDescription:
                $('#Description').val()?.trim() || null,

            trainingObjective:
                $('#Objective').val()?.trim() || null,

            coCode:
                $('#CoCode').val() || '',

            abrv: abrv,
                //$('#ABRV').val() || '',

            deptName: deptName,
                //$('#DeptName').val()?.trim() || null,

            eventType: eventType,

            trainerId: trainerId,

            trainerName: trainerName,

            budget:
                $('#Budget').val()
                    ? Number($('#Budget').val())
                    : 0,

            quota:
                $('#Quota').val()
                    ? Number($('#Quota').val())
                    : 1,

            venue:
                $('#Venue').val()?.trim() || null,

            eventStartDate:
                $('#StartDate').val(),

            eventEndDate:
                $('#EndDate').val(),

            sessionCount:
                $('#SessionNo').val()
                    ? Number($('#SessionNo').val())
                    : null,

            durationHours:
                $('#HoursPerSession').val()
                    ? Number($('#HoursPerSession').val())
                    : null,

            participants:
                TrainingEventCreate.state.participants.map(function (item) {

                    return {
                        employeeCode:
                            item.employeeCode,

                        name:
                            item.fullName ||
                            item.name,

                        abrv:
                            item.departmentCode || '',
                            
                        deptName:
                            item.departmentName || ''
                    };
                })
        };
    }

    /*========================================================
    * Validation
    *========================================================*/
    function validateCreateForm() {

        if (!$('#CourseId').val()) {
            showWarning('Please select a course.', 'Course');
            return false;
        }

       
        if (!$('#CoCode').val()) {
            showWarning('Please select a company.','Company');
            return false;
        }

        if (!$('#ABRV').val()) {
            showWarning('Please select a department.','Department Code');
            return false;
        }

        if (!$('#TrainingTitle').val()?.trim()) {
            showWarning('Training title is required.','Training Title');
            return false;
        }

        if (!$('#SessionNo').val()) {
            showWarning('Session count is required.','Session Number');
            return false;
        }

        if (!$('#Quota').val()) {
            showWarning('Participant quota is required.','Participant Quota');
            return false;
        }   

        if (!$('#StartDate').val()) {
            showWarning('Start date is required.','Start Date');
            return false;
        }

        if (!$('#EndDate').val()) {
            showWarning('End date is required.','End Date');
            return false;
        }

        const startDate = $('#StartDate').val();
        const endDate = $('#EndDate').val();

        if (startDate && endDate) {

            const start = new Date(startDate);
            const end = new Date(endDate);

            if (end < start) {

                showWarning(
                    'End date must be greater than or equal to start date.',
                    'Invalid Date'
                );

                return false;
            }
        }

        if (TrainingEventCreate.state.participants.length === 0) {
            showWarning('Please add at least one participant.','Participant');
            return false;
        }

        const quota =
            Number($('#Quota').val()) || 0;

        const participantCount =
            TrainingEventCreate.state.participants.length;

        if (quota <= 0) {

            showWarning(
                'Participant quota must be greater than zero.','Participant Quota'
            );

            return false;
        }

        if (participantCount > quota) {

            showWarning(
                'Number of participants cannot exceed the participant quota.','Participant Quota'
            );

            return false;
        }

        const trainerType =
            $('#TrainerType').val();

        if (trainerType === 'Internal') {

            if (!$('#TrainerEmployeeCode').val()) {
                showWarning('Please select an internal trainer.','Internal Trainer');
                return false;
            }

        } else {

            if (!$('#TrainerSearch').val()?.trim()) {
                showWarning('External trainer name is required.','External Trainer');
                return false;
            }
        }

        return true;
    }

    /*========================================================
    * Save Event
    *========================================================*/
    async function saveEvent() {

        console.log('[CREATE] saveEvent() CALLED');
        
        if (!validateCreateForm()) {
            return;
        }

        console.log('[CREATE] validation PASSED');

        const request = buildCreateRequest();

        console.log('========== CREATE DEBUG ==========');
        console.log('request =', request);
        console.log('json =', JSON.stringify(request));
        console.log('==================================');

        console.log('[TrainingEvent] Create request:', request);

        const token =
            $('#trainingEventForm input[name="__RequestVerificationToken"]')
                .val();

        if (!token) {
            showWarning('Anti-forgery token was not found.','Error');
            return;
        }

        const $button = $('#btnSaveEvent');

        $button.prop('disabled', true);

        const originalHtml = $button.html();

        $button.html(`
        <span class="spinner-border spinner-border-sm me-1"
              role="status"
              aria-hidden="true"></span>
        Saving...
    `);

        try {

            const response = await $.ajax({
                url: trainingEventCreateUrls.save,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(request),
                headers: {
                    'RequestVerificationToken': token
                }
            });

            console.log(
                '[TrainingEvent] Create response:',
                response
            );

            if (!response.success) {
                throw new Error(
                    response.message ||
                    'Failed to create training event.'
                );
            }

            const event = response.data;

            
            Swal.fire({
                title: "Create Training Event",
                text: `Training event ${event.eventCode} created successfully.`,
                confirmButtonText: "OK"
            }).then((result) => {
                /* Read more about isConfirmed, isDenied below */
                if (result.isConfirmed) window.location.href =
                    trainingEventCreateUrls.index;
            });
            

        } catch (error) {

            console.error('[TrainingEvent] Create error:', error);
            console.error('[TrainingEvent] HTTP status:', error.status);
            console.error('[TrainingEvent] Response JSON:', error.responseJSON);
            console.error('[TrainingEvent] Response text:', error.responseText);

            let message = 'Failed to save training event.';

            if (error.responseJSON?.message) {
                message = error.responseJSON.message;
            }
            else if (error.responseJSON?.errors) {
                message = JSON.stringify(
                    error.responseJSON.errors,
                    null,
                    2
                );
            }
            else if (error.responseText) {
                message = error.responseText;
            }
            else if (error.message) {
                message = error.message;
            }

            alert(message);

            $button.prop('disabled', false);
            $button.html(originalHtml);
        }
    }

})(jQuery);