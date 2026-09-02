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
                return;
            }

            const exists =
                self.state.participants.some(
                    function (item) {

                        return (
                            item.employeeCode
                                .toLowerCase() ===
                            employeeCode.toLowerCase()
                        );

                    }
                );

            if (exists) {

                Swal.fire({
                    title: 'Participant',
                    text: 'This employee has already been added.',
                    confirmButtonText: 'OK'
                });

                return;
            }

            self.state.participants.push({

                employeeCode: employeeCode,

                fullName: fullName,

                departmentCode: departmentCode,

                departmentName: departmentName

            });

            self.renderParticipants();

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

            Swal.fire({

                title: 'Remove Participant?',

                text:
                    'This participant will be removed from the event.',

                icon: 'warning',

                showCancelButton: true,

                confirmButtonText: 'Remove',

                cancelButtonText: 'Cancel'

            }).then(function (result) {

                if (!result.isConfirmed) {
                    return;
                }

                self.state.participants.splice(
                    index,
                    1
                );

                self.renderParticipants();

            });

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

                return;
            }

            $empty.addClass('d-none');

            $.each(
                self.state.participants,
                function (
                    index,
                    participant
                ) {

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

        },


        validateForm: function () {

            const self = this;

            const start =
                $('#EventStartDate').val();

            const end =
                $('#EventEndDate').val();

            if (!$('#CoCode').val()) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Company is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (!$('#ABRV').val()) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Department is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (!start) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Start date is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (!end) {

                Swal.fire({
                    title: 'Validation',
                    text: 'End date is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (new Date(end) < new Date(start)) {

                Swal.fire({
                    title: 'Validation',
                    text: 'End date cannot be earlier than start date.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (
                !$('#ParticipantQuota').val() ||
                Number($('#ParticipantQuota').val()) <= 0
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Participant quota is required.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (
                self.state.participants.length === 0
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Please add at least one participant.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            if (
                self.state.participants.length >
                Number($('#ParticipantQuota').val())
            ) {

                Swal.fire({
                    title: 'Validation',
                    text: 'Number of participants cannot exceed the participant quota.',
                    confirmButtonText: 'OK'
                });

                return false;
            }

            const trainerType =
                $('#TrainerType').val();

            if (trainerType === 'Internal') {

                if (!$('#TrainerId').val()) {

                    Swal.fire({
                        title: 'Validation',
                        text: 'Please select an internal trainer.',
                        confirmButtonText: 'OK'
                    });

                    return false;
                }

            } else {

                if (
                    !$('#TrainerSearch')
                        .val()
                        .trim()
                ) {

                    Swal.fire({
                        title: 'Validation',
                        text: 'External trainer name is required.',
                        confirmButtonText: 'OK'
                    });

                    return false;
                }

            }

            return true;
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