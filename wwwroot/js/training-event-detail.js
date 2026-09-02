"use strict";

$(function () {

    // =========================================================
    // CONFIGURATION CHECK
    // =========================================================

    if (typeof trainingEventDetailUrls === "undefined") {

        console.error(
            "[TrainingEvent] trainingEventDetailUrls is not defined."
        );

        return;
    }

    if (typeof trainingEventDetail === "undefined") {

        console.error(
            "[TrainingEvent] trainingEventDetail is not defined."
        );

        return;
    }


    // =========================================================
    // BACK TO INDEX
    // =========================================================

    $("#btnBackToEventIndex").on("click", function () {

        window.location.href =
            trainingEventDetailUrls.index;

    });


    // =========================================================
    // EDIT
    // =========================================================

    $("#btnEditEvent").on("click", function () {

        if (!trainingEventDetail.canEdit) {

            console.warn(
                "[TrainingEvent] Edit is not allowed."
            );

            return;
        }

        const id =
            trainingEventDetail.id;

        if (!id) {

            console.error(
                "[TrainingEvent] Training Event ID is missing."
            );

            return;
        }

        const url =
            trainingEventDetailUrls.edit +
            "?id=" +
            encodeURIComponent(id);

        window.location.href = url;

    });


    // =========================================================
    // DELETE
    // =========================================================

    $("#btnDeleteEvent").on("click", function () {

        if (!trainingEventDetail.canEdit) {

            console.warn(
                "[TrainingEvent] Delete is not allowed."
            );

            return;
        }

        Swal.fire({
            title: "Delete Training Event?",
            text: "This training event and its participants will be deleted.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Yes, Delete",
            cancelButtonText: "Cancel",
            reverseButtons: true
        }).then(function (result) {

            if (!result.isConfirmed) {
                return;
            }

            $("#deleteEventForm").trigger("submit");
        });
    });


    // =========================================================
    // ACTION MODAL
    // =========================================================

    $("#btnEventAction").on("click", function () {

        if (!trainingEventDetail.canAction) {

            console.warn(
                "[TrainingEvent] Action is not allowed."
            );

            return;
        }

        const modalElement =
            document.getElementById("eventActionModal");

        if (!modalElement) {

            console.error(
                "[TrainingEvent] Action modal not found."
            );

            return;
        }

        const modal =
            bootstrap.Modal.getOrCreateInstance(
                modalElement
            );

        modal.show();

    });


    // =========================================================
    // APPROVE
    // =========================================================

    $("#btnApproveEvent").on("click", function () {

        if (!trainingEventDetail.canAction) {
            return;
        }

        const confirmed =
            window.confirm(
                "Are you sure you want to approve this training event?"
            );

        if (!confirmed) {
            return;
        }

        /*
         * Approve API akan kita implementasikan
         * setelah Edit/Delete selesai.
         */

        console.log(
            "[TrainingEvent] Approve requested:",
            trainingEventDetail.id
        );

    });


    // =========================================================
    // OPEN REJECT MODAL
    // =========================================================

    $("#btnRejectEvent").on("click", function () {

        if (!trainingEventDetail.canAction) {
            return;
        }

        const actionModalElement =
            document.getElementById("eventActionModal");

        if (actionModalElement) {

            const actionModal =
                bootstrap.Modal.getInstance(
                    actionModalElement
                );

            if (actionModal) {
                actionModal.hide();
            }
        }

        $("#rejectReason")
            .val("")
            .removeClass("is-invalid");

        const rejectModalElement =
            document.getElementById("rejectEventModal");

        if (!rejectModalElement) {

            console.error(
                "[TrainingEvent] Reject modal not found."
            );

            return;
        }

        const rejectModal =
            bootstrap.Modal.getOrCreateInstance(
                rejectModalElement
            );

        rejectModal.show();

    });


    // =========================================================
    // REJECT REASON VALIDATION
    // =========================================================

    $("#rejectReason").on("input", function () {

        const value =
            $.trim($(this).val());

        if (value) {

            $(this)
                .removeClass("is-invalid");

        }

    });


    // =========================================================
    // CONFIRM REJECT
    // =========================================================

    $("#btnConfirmReject").on("click", function () {

        if (!trainingEventDetail.canAction) {
            return;
        }

        const reason =
            $.trim(
                $("#rejectReason").val()
            );

        if (!reason) {

            $("#rejectReason")
                .addClass("is-invalid")
                .focus();

            return;
        }

        $("#rejectReason")
            .removeClass("is-invalid");

        /*
         * Reject API akan kita implementasikan
         * setelah Edit/Delete selesai.
         */

        console.log(
            "[TrainingEvent] Reject requested:",
            {
                id: trainingEventDetail.id,
                reason: reason
            }
        );

    });


    //=========================================================
    //POST
    //========================================================
    $("#btnPostEvent").on("click", function () {

        if (!trainingEventDetail.canAction ||
            trainingEventDetail.availableAction !== "Post") {
            return;
        }

        Swal.fire({
            title: "Post Training Event?",
            text: "The training event will be submitted for approval.",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes, Post",
            cancelButtonText: "Cancel",
            reverseButtons: true
        }).then(function (result) {

            if (!result.isConfirmed) {
                return;
            }

            executeEventAction(
                "Request for Approve",
                ""
            );
        });
    });

    //=========================================================
    //Approve
    //========================================================
    $("#btnApproveEvent").on("click", function () {

        if (!trainingEventDetail.canAction ||
            trainingEventDetail.availableAction !== "ApproveReject") {
            return;
        }

        Swal.fire({
            title: "Approve Training Event?",
            text: "The training event will move to the next workflow state.",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes, Approve",
            cancelButtonText: "Cancel",
            reverseButtons: true
        }).then(function (result) {

            if (!result.isConfirmed) {
                return;
            }

            executeEventAction(
                "Approve",
                ""
            );
        });
    });

    //=========================================================
    //Reject
    //========================================================
    $("#btnRejectEvent").on("click", function () {

        if (!trainingEventDetail.canAction ||
            trainingEventDetail.availableAction !== "ApproveReject") {
            return;
        }

        $("#rejectReason")
            .val("")
            .removeClass("is-invalid");

        const modalElement =
            document.getElementById("rejectEventModal");

        const modal =
            bootstrap.Modal.getOrCreateInstance(modalElement);

        modal.show();
    });

    //=========================================================
    //Confirm Reject
    //========================================================
    $("#btnConfirmReject").on("click", function () {

        const reason =
            $("#rejectReason").val().trim();

        if (!reason) {

            $("#rejectReason")
                .addClass("is-invalid")
                .focus();

            return;
        }

        $("#rejectReason")
            .removeClass("is-invalid");

        const modalElement =
            document.getElementById("rejectEventModal");

        const modal =
            bootstrap.Modal.getOrCreateInstance(modalElement);

        modal.hide();

        executeEventAction(
            "Reject",
            reason
        );
    });


    //=========================================================
    //Execute Event Action
    //========================================================
    function executeEventAction(actionName, remarks) {

        $.ajax({
            url: trainingEventDetailUrls.eventAction,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                id: trainingEventDetail.id,
                actionName: actionName,
                remarks: remarks || ""
            }),
            success: function (response) {

                if (!response || !response.success) {

                    Swal.fire({
                        icon: "error",
                        title: "Action Failed",
                        text: response?.message ||
                            "Unable to execute the action."
                    });

                    return;
                }

                Swal.fire({
                    icon: "success",
                    title: "Success",
                    text: response.message,
                    timer: 1200,
                    showConfirmButton: false
                }).then(function () {

                    window.location.reload();

                });
            },
            error: function (xhr) {

                let message =
                    "An unexpected error occurred.";

                if (xhr.responseJSON &&
                    xhr.responseJSON.message) {

                    message =
                        xhr.responseJSON.message;
                }

                Swal.fire({
                    icon: "error",
                    title: "Action Failed",
                    text: message
                });
            }
        });
    }







});