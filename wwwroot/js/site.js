// Helper functions
function showError(message) {
    $('#validation-error').removeClass('d-none').html('<i class="fas fa-exclamation-triangle me-2"></i>' + message);
}

function clearError() {
    $('#validation-error').addClass('d-none').text('');
}

function updateToggleLabel(isActive) {
    const label = $('#activeLabel');
    if (isActive) {
        label.text('Active').removeClass('text-danger').addClass('text-success');
    } else {
        label.text('Inactive').removeClass('text-success').addClass('text-danger');
    }
}

function showNotification(message, type) {
    // Simple alert for now - you can replace with toast notifications
    //alert(type);
    if (type === "success") {
        //alert(_msg);
        toastr.success(message, 'Success');
    } else if (type === "error") {
        toastr.error(message, 'Error');
    } else if (type === "info") {
        toastr.info(message, 'Information');
    } else if (type === "warning") {
        toastr.warning(message, 'Warning');
    }
}

