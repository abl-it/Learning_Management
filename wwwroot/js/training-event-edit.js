(function ($) {

    'use strict';

    console.log('===== training-event-edit.js LOADED =====');

    const TrainingEventEdit = {

        urls: window.trainingEventEditUrls || {},

        state: {
            selectedTrainer: null,
            participants: []
        },

        init: function () {

            console.log('[EDIT] init START');

            this.urls = window.trainingEventEditUrls || {};

            this.state.participants =
                this.normalizeParticipants(
                    window.trainingEventEditInitial?.participants || []
                );

            this.bindEvents();

            this.initializeTrainer();

            this.renderParticipants();

            console.log(
                '[EDIT] Initial participants:',
                this.state.participants
            );

            console.log('[EDIT] init END');
        },


        normalizeParticipants: function (participants) {

            return (participants || []).map(function (item) {

                return {
                    employeeCode:
                        item.employeeCode ||
                        item.EmployeeCode ||
                        '',

                    fullName:
                        item.name ||
                        item.Name ||
                        '',

                    departmentCode:
                        item.abrv ||
                        item.ABRV ||
                        '',

                    departmentName:
                        item.deptName ||
                        item.DeptName ||
                        ''
                };

            });

        },


        bindEvents: function () {

            const self = this;

            $('#TrainerType')
                .off('change.trainingEventEdit')
                .on(
                    'change.trainingEventEdit',
                    function () {

                        const type =
                            $(this).val();

                        if (type === 'Internal') {

                            $('#TrainerId').val('');

                            $('#TrainerName').val('');

                            $('#TrainerSearch').val('');

                            self.initInternalTrainerTypeahead();

                            return;
                        }

                        if (type === 'External') {

                            $('#TrainerId').val('');

                            $('#TrainerName').val('');

                            $('#TrainerSearch').val('');

                            self.initExternalTrainer();

                        }

                    }
                );


            $('#btnAddParticipant')
                .off('click.trainingEventEdit')
                .on(
                    'click.trainingEventEdit',
                    function (e) {

                        e.preventDefault();

                        self.openParticipantModal();

                    }
            );

            $('#ParticipantQuota')
                .off('change.trainingQuota')
                .on(
                    'change.trainingQuota',
                    function () {

                        self.validateQuotaChange();

                    }
                );


            $('#btnConfirmParticipant')
                .off('click.trainingEventEdit')
                .on(
                    'click.trainingEventEdit',
                    function (e) {

                        e.preventDefault();

                        self.addParticipant();

                    }
                );


            $('#btnSaveEvent')
                .off('click.trainingEventEdit')
                .on(
                    'click.trainingEventEdit',
                    function (e) {

                        e.preventDefault();

                        self.submitForm();

                    }
            );

            
        },


        initializeTrainer: function () {

            const self = this;

            const $trainer = $('#TrainerSearch');

            if (!$trainer.length) {
                return;
            }

            const eventType =
                window.trainingEventEditInitial?.eventType || '';

            const trainerId =
                $('#TrainerId').val();

            const trainerName =
                $('#TrainerName').val();

            /*
             * Initial state dari database.
             *
             * I = Internal
             * E = External
             */
            if (eventType === 'I') {

                $('#TrainerType')
                    .val('Internal');

                $trainer.val(
                    trainerId
                        ? trainerId + ' - ' + (trainerName || '')
                        : ''
                );

                self.initInternalTrainerTypeahead();

                return;
            }

            /*
             * E = External
             */
            if (eventType === 'E') {

                $('#TrainerType')
                    .val('External');

                $trainer.val(
                    trainerName || ''
                );

                self.initExternalTrainer();

                return;
            }

            /*
             * Fallback jika EventType kosong.
             */
            if (trainerId) {

                $('#TrainerType')
                    .val('Internal');

                $trainer.val(
                    trainerId +
                    ' - ' +
                    (trainerName || '')
                );

                self.initInternalTrainerTypeahead();

            } else {

                $('#TrainerType')
                    .val('External');

                $trainer.val(
                    trainerName || ''
                );

                self.initExternalTrainer();
            }
        },


        destroyTrainerTypeahead: function () {

            const $trainer = $('#TrainerSearch');

            if ($trainer.data('typeahead')) {

                $trainer.typeahead('destroy');

            }

            $trainer.off(
                'input.trainingTrainerEdit'
            );

        },


        initInternalTrainerTypeahead: function () {

            const self = this;

            const $trainer =
                $('#TrainerSearch');

            if (!$trainer.length) {
                return;
            }

            self.destroyTrainerTypeahead();

            const url =
                self.urls.searchEmployees;

            if (!url) {

                console.error(
                    '[EDIT] SearchEmployees URL tidak tersedia.'
                );

                return;
            }

            $trainer
                .prop('readonly', false)
                .attr(
                    'placeholder',
                    'Search employee...'
                );

            $trainer.typeahead({

                minLength: 2,

                items: 10,

                autoSelect: false,

                source: function (
                    query,
                    process
                ) {

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
                                '[EDIT] Trainer result:',
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
                                '[EDIT] Trainer search failed:',
                                xhr.status,
                                status,
                                error,
                                xhr.responseText
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
                        '[EDIT] Trainer selected:',
                        item
                    );

                    $('#TrainerId')
                        .val(
                            item.employeeCode || ''
                        );

                    $('#TrainerName')
                        .val(
                            item.fullName || ''
                        );

                    $trainer.val(
                        item.employeeCode +
                        ' - ' +
                        item.fullName
                    );

                }

            });

        },


        initExternalTrainer: function () {

            const $trainer =
                $('#TrainerSearch');

            this.destroyTrainerTypeahead();

            $trainer
                .prop('readonly', false)
                .attr(
                    'placeholder',
                    'Enter trainer name...'
                );

            $trainer
                .off('input.trainingTrainerEdit')
                .on(
                    'input.trainingTrainerEdit',
                    function () {

                        $('#TrainerId').val('');

                        $('#TrainerName')
                            .val(
                                $(this).val()
                            );

                    }
                );

        },


        openParticipantModal: function () {

            this.resetParticipantModal();

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

                TrainingEventEdit
                    .initParticipantTypeahead();

                $('#ParticipantSearch')
                    .trigger('focus');

            }, 300);

        },


        initParticipantTypeahead: function () {

            const self = this;

            const $input =
                $('#ParticipantSearch');

            if (!$input.length) {
                return;
            }

            if (typeof $.fn.typeahead !== 'function') {

                console.error(
                    '[EDIT] bootstrap3-typeahead tidak tersedia.'
                );

                return;
            }

            if ($input.data('typeahead')) {

                $input.typeahead('destroy');

            }

            const url =
                self.urls.searchEmployees;

            if (!url) {

                console.error(
                    '[EDIT] SearchEmployees URL tidak tersedia.'
                );

                return;
            }

            $input.typeahead({

                minLength: 2,

                items: 10,

                autoSelect: false,

                source: function (
                    query,
                    process
                ) {

                    $.ajax({

                        url: url,

                        type: 'GET',

                        dataType: 'json',

                        data: {
                            term: query
                        }

                    })
                        .done(function (response) {

                            process(
                                Array.isArray(response)
                                    ? response
                                    : []
                            );

                        })
                        .fail(function (
                            xhr
                        ) {

                            console.error(
                                '[EDIT] Participant search failed:',
                                xhr.status,
                                xhr.responseText
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

                    $('#ParticipantEmployeeCode')
                        .val(
                            item.employeeCode || ''
                        );

                    $('#ParticipantName')
                        .val(
                            item.fullName || ''
                        );

                    $('#ParticipantDepartment')
                        .val(
                            item.abrv || ''
                        );

                    $('#ParticipantDepartmentName')
                        .val(
                            item.deptName || ''
                        );

                    $('#participantSelectedCode')
                        .text(
                            item.employeeCode || ''
                        );

                    $('#participantSelectedName')
                        .text(
                            item.fullName || ''
                        );

                    $('#participantSelectedDepartment')
                        .text(
                            item.deptName || '-'
                        );

                    $('#participantSelectedInfo')
                        .removeClass('d-none');

                    $('#btnConfirmParticipant')
                        .prop('disabled', false);

                }

            });

        },


        resetParticipantModal: function () {

            $('#ParticipantSearch').val('');

            $('#ParticipantEmployeeCode').val('');

            $('#ParticipantName').val('');

            $('#ParticipantDepartment').val('');

            $('#ParticipantDepartmentName').val('');

            $('#participantSelectedCode').text('');

            $('#participantSelectedName').text('');

            $('#participantSelectedDepartment')
                .text('');

            $('#participantSelectedInfo')
                .addClass('d-none');

            $('#btnConfirmParticipant')
                .prop('disabled', true);

        },


        addParticipant: function () {

            const self = this;

            const participants =
                self.state.participants;

            const quota =
                Number($('#ParticipantQuota').val()) || 0;

            /*
             * =====================================================
             * QUOTA VALIDATION
             * =====================================================
             */

            if (quota <= 0) {

                Swal.fire({
                    title: 'Participant Quota',
                    text:
                        'Please enter a participant quota first.',
                    confirmButtonText: 'OK'
                });

                return;
            }

            if (participants.length >= quota) {

                Swal.fire({
                    title: 'Participant Limit',
                    text:
                        'Participant quota has been reached. Please increase the quota before adding another participant.',
                    confirmButtonText: 'OK'
                });

                self.updateAddParticipantButton();

                return;
            }

            /*
             * =====================================================
             * SELECTED EMPLOYEE
             * =====================================================
             */

            const employeeCode =
                $('#ParticipantEmployeeCode')
                    .val()
                    .trim();

            const fullName =
                $('#ParticipantName')
                    .val()
                    .trim();

            const departmentCode =
                $('#ParticipantDepartment')
                    .val()
                    .trim();

            const departmentName =
                $('#ParticipantDepartmentName')
                    .val()
                    .trim();

            if (!employeeCode) {

                Swal.fire({
                    title: 'Participant',
                    text:
                        'Please select an employee.',
                    confirmButtonText: 'OK'
                });

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

                Swal.fire({
                    title: 'Participant',
                    text:
                        'This employee has already been added.',
                    confirmButtonText: 'OK'
                });

                return;
            }

            /*
             * =====================================================
             * ADD
             * =====================================================
             */

            participants.push({

                employeeCode: employeeCode,

                fullName: fullName,

                departmentCode: departmentCode,

                departmentName: departmentName

            });

            self.renderParticipants();

            self.updateAddParticipantButton();

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

            self.resetParticipantModal();
        },


        removeParticipant: function (index) {

            const self = this;

            if (
                index < 0 ||
                index >= self.state.participants.length
            ) {
                return;
            }

            self.state.participants.splice(
                index,
                1
            );

            self.renderParticipants();
        },


        renderParticipants: function () {

            const self = this;

            const $tbody =
                $('#participantTableBody');

            const $empty =
                $('#participantEmpty');

            $tbody.empty();

            $('#participantCount')
                .text(
                    self.state.participants.length
                );

            if (
                self.state.participants.length === 0
            ) {

                $empty.removeClass('d-none');

                self.updateAddParticipantButton();

                return;
            }

            $empty.addClass('d-none');

            $.each(
                self.state.participants,
                function (index, participant) {

                    const $row =
                        $('<tr>');

                    $('<td>')
                        .addClass('text-center')
                        .text(index + 1)
                        .appendTo($row);

                    $('<td>')
                        .text(
                            participant.employeeCode
                        )
                        .appendTo($row);

                    $('<td>')
                        .text(
                            participant.fullName
                        )
                        .appendTo($row);

                    $('<td>')
                        .text(
                            participant.departmentCode || '-'
                        )
                        .appendTo($row);

                    $('<td>')
                        .text(
                            participant.departmentName || '-'
                        )
                        .appendTo($row);

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

                                    self.removeParticipant(
                                        index
                                    );

                                }
                            );

                    $('<td>')
                        .addClass('text-center')
                        .append($remove)
                        .appendTo($row);

                    $tbody.append($row);

                }
            );

            self.updateAddParticipantButton();
        },


        validateQuotaChange: function () {

            const $quota =
                $('#ParticipantQuota');

            const quota =
                Number($quota.val()) || 0;

            const participantCount =
                this.state.participants.length;

            if (quota <= 0) {

                return;
            }

            if (quota < participantCount) {

                Swal.fire({
                    title: 'Invalid Participant Quota',
                    text:
                        'Participant quota cannot be less than the number of participants already added.',
                    confirmButtonText: 'OK'
                });

                /*
                 * Kembalikan quota ke jumlah
                 * participant yang sudah ada.
                 */
                $quota.val(participantCount);

            }

            this.updateAddParticipantButton();
        },

        validateForm: function () {

            const self = this;

            /*
             * =====================================================
             * BASIC DATA
             * =====================================================
             */

            const courseId =
                Number($('#CourseId').val()) || 0;

            const trainingCategoryId =
                Number($('#TrainingCategoryId').val()) || 0;

            const trainingTitle =
                $('#TrainingTitle').val()?.trim();

            const coCode =
                $('#CoCode').val()?.trim();

            const abrv =
                $('#ABRV').val()?.trim();

            const eventType =
                $('#TrainerType').val();

            const trainerSearch =
                $('#TrainerSearch').val()?.trim();

            const trainerId =
                $('#TrainerId').val()?.trim();

            const trainerName =
                $('#TrainerName').val()?.trim();

            const participantQuota =
                Number($('#ParticipantQuota').val()) || 0;

            const sessionCount =
                Number($('#SessionCount').val()) || 0;

            const durationHours =
                Number($('#DurationHours').val()) || 0;

            const eventStartDate =
                $('#EventStartDate').val();

            const eventEndDate =
                $('#EventEndDate').val();

            const venue =
                $('#Venue').val()?.trim();


            /*
             * =====================================================
             * COURSE
             * =====================================================
             */

            if (courseId <= 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Course is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * TRAINING CATEGORY
             * =====================================================
             */

            if (trainingCategoryId <= 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Training category is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * TRAINING TITLE
             * =====================================================
             */

            if (!trainingTitle) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Training title is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * COMPANY
             * =====================================================
             */

            if (!coCode) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Company is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * DEPARTMENT
             * =====================================================
             */

            if (!abrv) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Department is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * TRAINER TYPE
             * =====================================================
             */

            if (
                eventType !== 'Internal' &&
                eventType !== 'External'
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Please select trainer type.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * TRAINER
             * =====================================================
             */

            if (!trainerSearch && !trainerName) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Trainer is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * INTERNAL TRAINER
             *
             * Internal trainer must have Employee Code.
             * =====================================================
             */

            if (
                eventType === 'Internal' &&
                !trainerId
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Please select an internal trainer from the employee list.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * PARTICIPANT QUOTA
             * =====================================================
             */

            if (participantQuota <= 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Participant quota must be greater than zero.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * PARTICIPANTS
             * =====================================================
             */

            const participantCount =
                self.state.participants.length;


            /*
             * At least one participant is required.
             */

            if (participantCount === 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Please add at least one participant.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * Participant count cannot exceed quota.
             */

            if (participantCount > participantQuota) {

                Swal.fire({
                    title: 'Validation',
                    text:
                        'Number of participants cannot exceed the participant quota.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * SESSION
             * =====================================================
             */

            if (sessionCount <= 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Session count must be greater than zero.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * DURATION
             * =====================================================
             */

            if (durationHours <= 0) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Hours / Session must be greater than zero.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * START DATE
             * =====================================================
             */

            if (!eventStartDate) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Start date is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * END DATE
             * =====================================================
             */

            if (!eventEndDate) {

                Swal.fire({
                    title: 'Validation',
                    text: 'End date is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * DATE COMPARISON
             * =====================================================
             */

            const startDate =
                new Date(eventStartDate);

            const endDate =
                new Date(eventEndDate);

            if (
                Number.isNaN(startDate.getTime()) ||
                Number.isNaN(endDate.getTime())
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Invalid start or end date.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            if (endDate < startDate) {

                Swal.fire({
                    title: 'Validation',
                    text: 'End date cannot be earlier than start date.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * VENUE
             * =====================================================
             */

            if (!venue) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Venue is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * FINAL PARTICIPANT DUPLICATE CHECK
             * =====================================================
             */

            const employeeCodes =
                self.state.participants
                    .map(function (participant) {

                        return (
                            participant.employeeCode || ''
                        )
                            .trim()
                            .toLowerCase();

                    })
                    .filter(function (code) {

                        return code.length > 0;

                    });

            const uniqueEmployeeCodes =
                new Set(employeeCodes);

            if (
                employeeCodes.length !==
                uniqueEmployeeCodes.size
            ) {

                Swal.fire({
                    title: 'Validation',
                    text:
                        'Duplicate participants are not allowed.',
                    confirmButtonText: 'OK'
                });

                return false;
            }


            /*
             * =====================================================
             * FINAL RESULT
             * =====================================================
             */

            return true;
        },

        updateAddParticipantButton: function () {

            const $button =
                $('#btnAddParticipant');

            if (!$button.length) {
                return;
            }

            const quota =
                Number($('#ParticipantQuota').val()) || 0;

            const participantCount =
                this.state.participants.length;

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
        },

        submitForm: function () {

            const self = this;

            if (!self.validateForm()) {
                return;
            }

            /*
             * Karena Controller Edit menerima
             * TrainingEventEditDto melalui form POST,
             * kita masukkan participant sebagai indexed fields.
             */

            const $form =
                $('#trainingEventEditForm');

            /*
             * Hapus participant fields lama
             * agar tidak duplicate.
             */
            $form.find(
                '.participant-hidden-field'
            ).remove();

            $.each(
                self.state.participants,
                function (
                    index,
                    participant
                ) {

                    $('<input>', {
                        type: 'hidden',
                        class: 'participant-hidden-field',
                        name:
                            `Participants[${index}].EmployeeCode`,
                        value:
                            participant.employeeCode
                    }).appendTo($form);

                    $('<input>', {
                        type: 'hidden',
                        class: 'participant-hidden-field',
                        name:
                            `Participants[${index}].Name`,
                        value:
                            participant.fullName
                    }).appendTo($form);

                    $('<input>', {
                        type: 'hidden',
                        class: 'participant-hidden-field',
                        name:
                            `Participants[${index}].ABRV`,
                        value:
                            participant.departmentCode
                    }).appendTo($form);

                    $('<input>', {
                        type: 'hidden',
                        class: 'participant-hidden-field',
                        name:
                            `Participants[${index}].DeptName`,
                        value:
                            participant.departmentName
                    }).appendTo($form);

                }
            );

            /*
             * Update hidden TrainerName.
             */
            if (
                $('#TrainerType').val() === 'External'
            ) {

                $('#TrainerId').val('');

                $('#TrainerName')
                    .val(
                        $('#TrainerSearch')
                            .val()
                            .trim()
                    );

            }

            const $button =
                $('#btnSaveEvent');

            $button.prop(
                'disabled',
                true
            );

            const originalHtml =
                $button.html();

            $button.html(`
                <span class="spinner-border spinner-border-sm me-1"
                      role="status"
                      aria-hidden="true"></span>
                Saving...
            `);


            const trainerType =
                $('#TrainerType').val();

            if (trainerType === 'Internal') {

                $('#EventType').val('I');

            } else {

                $('#EventType').val('E');

            }
            /*
             * Submit native form.
             */
            $form[0].submit();

        }

    };


    window.TrainingEventEdit =
        TrainingEventEdit;


    $(document).ready(function () {

        TrainingEventEdit.init();

    });

})(jQuery);