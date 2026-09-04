(function ($) {
    'use strict';

    function escapeHtml(value) {
        return String(value === null || value === undefined ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function getAntiForgeryToken() {
        return $('#realizationForm input[name="__RequestVerificationToken"]').val();
    }

    function formatFileSize(bytes) {
        if (!bytes || bytes <= 0) {
            return '-';
        }

        const units = ['B', 'KB', 'MB', 'GB'];
        let value = bytes;
        let unitIndex = 0;

        while (value >= 1024 && unitIndex < units.length - 1) {
            value = value / 1024;
            unitIndex++;
        }

        return value.toFixed(unitIndex === 0 ? 0 : 1) + ' ' + units[unitIndex];
    }

    function formatDate(value) {
        if (!value) {
            return '-';
        }

        const date = new Date(value);

        if (isNaN(date.getTime())) {
            return value;
        }

        const pad = function (n) {
            return n < 10 ? '0' + n : n;
        };

        return pad(date.getDate()) + '/' + pad(date.getMonth() + 1) + '/' + date.getFullYear() +
            ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes());
    }

    const RealizationPage = {

        state: {
            trainingEventId: 0,
            isPosted: false,
            participants: [],
            attachments: []
        },

        init: function () {

            const raw = document.getElementById('realizationInitialData');

            if (!raw) {
                console.error('[Realization] Initial data script not found.');
                return;
            }

            let data;

            try {
                data = JSON.parse(raw.textContent);
            } catch (e) {
                console.error('[Realization] Failed to parse initial data.', e);
                return;
            }

            this.state.trainingEventId = window.trainingEventRealizationTrainingEventId || 0;
            this.state.isPosted = window.trainingEventRealizationIsPosted === true;

            this.state.participants = (data.participants || []).map(function (p) {
                return {
                    plannedParticipantId: p.plannedParticipantId,
                    employeeCode: p.employeeCode,
                    name: p.name,
                    abrv: p.abrv,
                    deptName: p.deptName,
                    attendanceStatus: p.attendanceStatus || 'ABSENT',
                    remarks: p.remarks || '',
                    isWalkIn: !!p.isWalkIn
                };
            });

            this.state.attachments = data.attachments || [];

            this.renderParticipants();
            this.renderAttachments();
            this.bindEvents();
            this.initParticipantTypeahead();
            this.initHistoryCollapseToggle();
        },

        initHistoryCollapseToggle: function () {

            const collapseEl = document.getElementById('workflowHistoryBody');
            const chevron = document.getElementById('workflowHistoryChevron');

            if (!collapseEl || !chevron) {
                return;
            }

            collapseEl.addEventListener('show.bs.collapse', function () {
                chevron.classList.remove('bi-chevron-down');
                chevron.classList.add('bi-chevron-up');
            });

            collapseEl.addEventListener('hide.bs.collapse', function () {
                chevron.classList.remove('bi-chevron-up');
                chevron.classList.add('bi-chevron-down');
            });
        },

        renderParticipants: function () {

            const $body = $('#participantsTableBody');
            $body.empty();

            const isPosted = this.state.isPosted;

            if (this.state.participants.length === 0) {
                $body.append(
                    '<tr><td colspan="' + (isPosted ? 5 : 6) +
                    '" class="text-center text-muted py-3">No participants yet.</td></tr>'
                );

                $('#participantCountBadge').text(0);
                return;
            }

            this.state.participants.forEach(function (p, index) {

                const checked =
                    (p.attendanceStatus === 'PRESENT' || p.attendanceStatus === 'PARTIAL')
                        ? 'checked'
                        : '';

                const disabledAttr = isPosted ? 'disabled' : '';

                const walkInBadge = p.isWalkIn
                    ? '<span class="badge bg-info realization-walkin-badge ms-1">Walk-in</span>'
                    : '';

                const removeCell = isPosted
                    ? ''
                    : '<td>' +
                        (p.isWalkIn
                            ? '<button type="button" class="btn btn-sm btn-outline-danger btn-remove-participant" data-index="' + index + '"><i class="bi bi-x-lg"></i></button>'
                            : '') +
                        '</td>';

                const rowHtml =
                    '<tr data-index="' + index + '">' +
                    '<td>' + (index + 1) + '</td>' +
                    '<td>' + escapeHtml(p.name) + walkInBadge + '</td>' +
                    '<td>' + escapeHtml(p.deptName || '-') + '</td>' +
                    '<td class="text-center"><input type="checkbox" class="form-check-input participant-present-checkbox" data-index="' + index + '" ' + checked + ' ' + disabledAttr + ' /></td>' +
                    '<td><input type="text" class="form-control form-control-sm participant-remarks-input" data-index="' + index + '" ' + disabledAttr + ' /></td>' +
                    removeCell +
                    '</tr>';

                $body.append(rowHtml);

                $body.find('tr[data-index="' + index + '"] .participant-remarks-input').val(p.remarks || '');
            });

            $('#participantCountBadge').text(this.state.participants.length);
        },

        renderAttachments: function () {

            const $body = $('#attachmentsTableBody');
            $body.empty();

            if (this.state.attachments.length === 0) {
                $body.append(
                    '<tr><td colspan="5" class="text-center text-muted py-3">No attachments uploaded yet.</td></tr>'
                );

                return;
            }

            const isPosted = this.state.isPosted;

            this.state.attachments.forEach(function (a) {

                const downloadUrl =
                    window.trainingEventRealizationUrls.downloadAttachment +
                    '?attachmentId=' + a.trainingEventAttachmentId;

                const deleteBtn = isPosted
                    ? ''
                    : '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-attachment" data-id="' + a.trainingEventAttachmentId + '"><i class="bi bi-trash"></i></button>';

                const docTypeLabel =
                    a.documentType === 'Attendance' ? 'Attendance Sheet' : (a.documentType || 'Other');

                const rowHtml =
                    '<tr>' +
                    '<td><a href="' + downloadUrl + '" target="_blank"><i class="bi bi-file-earmark-arrow-down me-1"></i>' + escapeHtml(a.originalFileName) + '</a></td>' +
                    '<td>' + escapeHtml(docTypeLabel) + '</td>' +
                    '<td>' + formatFileSize(a.fileSize) + '</td>' +
                    '<td class="small">' + escapeHtml(a.createdBy) + '<br>' + formatDate(a.createdDate) + '</td>' +
                    '<td>' + deleteBtn + '</td>' +
                    '</tr>';

                $body.append(rowHtml);
            });
        },

        bindEvents: function () {

            const self = this;

            $('#btnBackToDetail').on('click', function () {
                window.location.href = window.trainingEventRealizationUrls.backPage;
            });

            $('#participantsTableBody').on('change', '.participant-present-checkbox', function () {
                const index = $(this).data('index');
                self.state.participants[index].attendanceStatus =
                    $(this).is(':checked') ? 'PRESENT' : 'ABSENT';
            });

            $('#participantsTableBody').on('input', '.participant-remarks-input', function () {
                const index = $(this).data('index');
                self.state.participants[index].remarks = $(this).val();
            });

            $('#participantsTableBody').on('click', '.btn-remove-participant', function () {
                const index = $(this).data('index');
                self.state.participants.splice(index, 1);
                self.renderParticipants();
            });

            $('#btnAddParticipant').on('click', function () {

                $('#RealizationParticipantSearch').val('');
                $('#RealizationParticipantEmployeeCode').val('');
                $('#RealizationParticipantName').val('');
                $('#RealizationParticipantDepartment').val('');
                $('#RealizationParticipantDepartmentName').val('');
                $('#realizationParticipantSelectedInfo').addClass('d-none');
                $('#btnConfirmRealizationParticipant').prop('disabled', true);

                const modal = bootstrap.Modal.getOrCreateInstance(
                    document.getElementById('realizationParticipantModal'));

                modal.show();
            });

            $('#btnConfirmRealizationParticipant').on('click', function () {

                const employeeCode = $('#RealizationParticipantEmployeeCode').val();
                const name = $('#RealizationParticipantName').val();

                if (!employeeCode) {
                    return;
                }

                const alreadyExists = self.state.participants.some(function (p) {
                    return p.employeeCode === employeeCode;
                });

                if (alreadyExists) {
                    showNotification('This employee is already in the attendance list.', 'warning');
                    return;
                }

                self.state.participants.push({
                    plannedParticipantId: null,
                    employeeCode: employeeCode,
                    name: name,
                    abrv: $('#RealizationParticipantDepartment').val(),
                    deptName: $('#RealizationParticipantDepartmentName').val(),
                    attendanceStatus: 'PRESENT',
                    remarks: '',
                    isWalkIn: true
                });

                self.renderParticipants();

                bootstrap.Modal.getInstance(
                    document.getElementById('realizationParticipantModal')).hide();
            });

            $('#btnSaveDraft').on('click', function () {
                self.saveRealization(false);
            });

            $('#btnPostToHrd').on('click', function () {

                Swal.fire({
                    title: 'Post to HRD?',
                    text: 'Once posted, the realization can no longer be edited.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, Post',
                    cancelButtonText: 'Cancel',
                    reverseButtons: true
                }).then(function (result) {

                    if (!result.isConfirmed) {
                        return;
                    }

                    self.saveRealization(true);
                });
            });

            $('#btnUploadAttachment').on('click', function () {
                self.uploadAttachments();
            });

            $('#btnCompleteRealization').on('click', function () {
                self.completeRealization();
            });

            $('#btnRejectRealization').on('click', function () {
                self.rejectRealization();
            });

            $('#attachmentsTableBody').on('click', '.btn-delete-attachment', function () {

                const id = $(this).data('id');

                Swal.fire({
                    title: 'Delete Attachment?',
                    text: 'This file will be permanently deleted.',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, Delete',
                    cancelButtonText: 'Cancel',
                    reverseButtons: true
                }).then(function (result) {

                    if (!result.isConfirmed) {
                        return;
                    }

                    self.deleteAttachment(id);
                });
            });
        },

        initParticipantTypeahead: function () {

            const $input = $('#RealizationParticipantSearch');

            if (!$input.length || typeof $.fn.typeahead !== 'function') {
                console.error('[Realization] Participant search input or typeahead plugin not available.');
                return;
            }

            const url = window.trainingEventRealizationUrls.searchEmployees;

            $input.typeahead({

                minLength: 2,
                items: 10,
                autoSelect: false,

                source: function (query, process) {

                    $.ajax({
                        url: url,
                        type: 'GET',
                        dataType: 'json',
                        data: { term: query }
                    })
                        .done(function (response) {
                            process(response || []);
                        })
                        .fail(function () {
                            process([]);
                        });
                },

                displayText: function (item) {
                    return item.employeeCode + ' - ' + item.fullName;
                },

                afterSelect: function (item) {

                    $('#RealizationParticipantEmployeeCode').val(item.employeeCode || '');
                    $('#RealizationParticipantName').val(item.fullName || '');
                    $('#RealizationParticipantDepartment').val(item.abrv || '');
                    $('#RealizationParticipantDepartmentName').val(item.deptName || '');

                    $('#realizationParticipantSelectedCode').text(item.employeeCode || '');
                    $('#realizationParticipantSelectedName').text(item.fullName || '');
                    $('#realizationParticipantSelectedDepartment').text(item.deptName || '-');

                    $('#realizationParticipantSelectedInfo').removeClass('d-none');
                    $('#btnConfirmRealizationParticipant').prop('disabled', false);
                }
            });
        },

        collectPayload: function (remarks) {

            const startTime = $('#StartTime').val();
            const endTime = $('#EndTime').val();

            return {
                trainingEventId: this.state.trainingEventId,
                realizationDate: $('#RealizationDate').val(),
                startTime: startTime ? startTime + ':00' : null,
                endTime: endTime ? endTime + ':00' : null,
                notes: $('#RealizationNotes').val(),
                remarks: remarks || '',
                participants: this.state.participants.map(function (p) {
                    return {
                        plannedParticipantId: p.plannedParticipantId,
                        employeeCode: p.employeeCode,
                        attendanceStatus: p.attendanceStatus,
                        remarks: p.remarks
                    };
                })
            };
        },

        saveRealization: function (post) {

            const self = this;

            if (!$('#RealizationDate').val() || !$('#StartTime').val() || !$('#EndTime').val()) {
                showNotification('Please fill in the realization date and time.', 'warning');
                return;
            }

            const token = getAntiForgeryToken();

            if (!token) {
                showNotification('Anti-forgery token was not found.', 'error');
                return;
            }

            const url = post
                ? window.trainingEventRealizationUrls.postToHrd
                : window.trainingEventRealizationUrls.saveDraft;

            const payload = this.collectPayload('');

            const $btn = post ? $('#btnPostToHrd') : $('#btnSaveDraft');
            const originalHtml = $btn.html();

            $btn.prop('disabled', true).html(
                '<span class="spinner-border spinner-border-sm me-1"></span> Saving...');

            $.ajax({
                url: url,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(payload),
                headers: {
                    'X-CSRF-TOKEN': token
                }
            })
                .done(function (response) {

                    if (!response || !response.success) {

                        showNotification(
                            (response && response.message) || 'Failed to save realization.',
                            'error');

                        $btn.prop('disabled', false).html(originalHtml);
                        return;
                    }

                    if (post) {

                        Swal.fire({
                            icon: 'success',
                            title: 'Posted to HRD',
                            text: response.message,
                            confirmButtonText: 'OK'
                        }).then(function () {
                            window.location.reload();
                        });

                    } else {

                        showNotification(response.message, 'success');
                        $btn.prop('disabled', false).html(originalHtml);
                    }
                })
                .fail(function (xhr) {

                    const message =
                        (xhr.responseJSON && xhr.responseJSON.message) ||
                        'An unexpected error occurred.';

                    showNotification(message, 'error');
                    $btn.prop('disabled', false).html(originalHtml);
                });
        },

        completeRealization: function () {

            const self = this;

            Swal.fire({
                title: 'Mark Realization as Complete?',
                html:
                    '<div class="text-start">' +
                    '<p>This confirms HRD has reviewed the realization. This cannot be undone.</p>' +
                    '<label class="form-label small mb-1">Remarks (optional)</label>' +
                    '<textarea id="swalCompleteRemarks" class="form-control form-control-sm" rows="3"></textarea>' +
                    '</div>',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes, Complete',
                cancelButtonText: 'Cancel',
                reverseButtons: true,
                focusConfirm: false,
                preConfirm: function () {
                    return $('#swalCompleteRemarks').val();
                }
            }).then(function (result) {

                if (!result.isConfirmed) {
                    return;
                }

                const token = getAntiForgeryToken();

                if (!token) {
                    showNotification('Anti-forgery token was not found.', 'error');
                    return;
                }

                $.ajax({
                    url: window.trainingEventRealizationUrls.completeRealization,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify({
                        trainingEventId: self.state.trainingEventId,
                        remarks: result.value || ''
                    }),
                    headers: {
                        'X-CSRF-TOKEN': token
                    }
                })
                    .done(function (response) {

                        if (!response || !response.success) {
                            showNotification(
                                (response && response.message) || 'Failed to complete realization.',
                                'error');
                            return;
                        }

                        Swal.fire({
                            icon: 'success',
                            title: 'Completed',
                            text: response.message,
                            confirmButtonText: 'OK'
                        }).then(function () {
                            window.location.reload();
                        });
                    })
                    .fail(function (xhr) {

                        const message =
                            (xhr.responseJSON && xhr.responseJSON.message) ||
                            'An unexpected error occurred.';

                        showNotification(message, 'error');
                    });
            });
        },

        rejectRealization: function () {

            const self = this;

            Swal.fire({
                title: 'Reject Realization?',
                html:
                    '<div class="text-start">' +
                    '<p>This sends the realization back to the creator to fix and resubmit.</p>' +
                    '<label class="form-label small mb-1">Reason <span class="text-danger">*</span></label>' +
                    '<textarea id="swalRejectRemarks" class="form-control form-control-sm" rows="3" placeholder="Explain what is missing or incorrect..."></textarea>' +
                    '</div>',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes, Reject',
                cancelButtonText: 'Cancel',
                reverseButtons: true,
                focusConfirm: false,
                preConfirm: function () {

                    const remarks = $('#swalRejectRemarks').val();

                    if (!remarks || !remarks.trim()) {
                        Swal.showValidationMessage('A reason is required.');
                        return false;
                    }

                    return remarks;
                }
            }).then(function (result) {

                if (!result.isConfirmed) {
                    return;
                }

                const token = getAntiForgeryToken();

                if (!token) {
                    showNotification('Anti-forgery token was not found.', 'error');
                    return;
                }

                $.ajax({
                    url: window.trainingEventRealizationUrls.rejectRealization,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify({
                        trainingEventId: self.state.trainingEventId,
                        remarks: result.value
                    }),
                    headers: {
                        'X-CSRF-TOKEN': token
                    }
                })
                    .done(function (response) {

                        if (!response || !response.success) {
                            showNotification(
                                (response && response.message) || 'Failed to reject realization.',
                                'error');
                            return;
                        }

                        Swal.fire({
                            icon: 'success',
                            title: 'Rejected',
                            text: response.message,
                            confirmButtonText: 'OK'
                        }).then(function () {
                            window.location.reload();
                        });
                    })
                    .fail(function (xhr) {

                        const message =
                            (xhr.responseJSON && xhr.responseJSON.message) ||
                            'An unexpected error occurred.';

                        showNotification(message, 'error');
                    });
            });
        },

        uploadAttachments: function () {

            const self = this;
            const filesInput = document.getElementById('AttachmentFiles');

            if (!filesInput.files || filesInput.files.length === 0) {
                showNotification('Please choose at least one file.', 'warning');
                return;
            }

            const token = getAntiForgeryToken();

            if (!token) {
                showNotification('Anti-forgery token was not found.', 'error');
                return;
            }

            const formData = new FormData();
            formData.append('trainingEventId', this.state.trainingEventId);
            formData.append('documentType', $('#AttachmentDocumentType').val());

            for (let i = 0; i < filesInput.files.length; i++) {
                formData.append('files', filesInput.files[i]);
            }

            const $btn = $('#btnUploadAttachment');
            const originalHtml = $btn.html();

            $btn.prop('disabled', true).html(
                '<span class="spinner-border spinner-border-sm me-1"></span> Uploading...');

            $.ajax({
                url: window.trainingEventRealizationUrls.uploadAttachment,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                headers: {
                    'X-CSRF-TOKEN': token
                }
            })
                .done(function (response) {

                    if (response && response.data && response.data.length) {
                        self.state.attachments = response.data.concat(self.state.attachments);
                        self.renderAttachments();
                        filesInput.value = '';
                    }

                    if (response && response.success) {
                        showNotification(response.message, 'success');
                    } else {
                        showNotification((response && response.message) || 'Upload failed.', 'error');
                    }
                })
                .fail(function (xhr) {

                    const message =
                        (xhr.responseJSON && xhr.responseJSON.message) || 'Upload failed.';

                    showNotification(message, 'error');
                })
                .always(function () {
                    $btn.prop('disabled', false).html(originalHtml);
                });
        },

        deleteAttachment: function (id) {

            const self = this;
            const token = getAntiForgeryToken();

            $.ajax({
                url: window.trainingEventRealizationUrls.deleteAttachment,
                type: 'POST',
                data: { attachmentId: id },
                headers: {
                    'X-CSRF-TOKEN': token
                }
            })
                .done(function (response) {

                    if (response && response.success) {

                        self.state.attachments = self.state.attachments.filter(function (a) {
                            return a.trainingEventAttachmentId !== id;
                        });

                        self.renderAttachments();
                        showNotification(response.message, 'success');

                    } else {

                        showNotification(
                            (response && response.message) || 'Failed to delete attachment.',
                            'error');
                    }
                })
                .fail(function () {
                    showNotification('Failed to delete attachment.', 'error');
                });
        }
    };

    $(document).ready(function () {
        RealizationPage.init();
    });

})(jQuery);
