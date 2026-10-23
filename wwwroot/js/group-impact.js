/* =========================================================
   Group Impact (Training Matrix - Phase 1D)
   Dependensi: jQuery, Bootstrap 5, SweetAlert2
   Konfigurasi: window.groupImpactCfg (Views/CourseGroup/Index.cshtml)
   ========================================================= */

(function () {
    'use strict';

    const cfg = window.groupImpactCfg;
    const urls = cfg.urls;
    const canEdit = cfg.canEdit === true;

    let modal = null;
    let current = null;          // { id, name }
    let items = [];

    /* ---------- helpers ---------- */

    function esc(value) { return $('<div>').text(value ?? '').html(); }

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val();
    }

    function toast(icon, text) {
        return Swal.fire({ icon: icon, text: text, confirmButtonText: 'OK' });
    }

    function ajaxError(xhr) {
        toast('error', xhr?.responseJSON?.message || 'Terjadi kesalahan pada server.');
    }

    function statusBadge(s) {
        const cls = { Approved: 'bg-success', Draft: 'bg-secondary', Rejected: 'bg-danger' }[s] || 'bg-warning text-dark';
        return `<span class="badge ${cls}">${esc(s)}</span>`;
    }

    function modeText(it) {
        switch (it.mode) {
            case 'Direct': return 'Ubah matrix Draft langsung';
            case 'NewCr': return 'Buat Change Request baru';
            case 'AppendCr': return `Tambah ke Change Request #${it.changeRequestId}`;
            default: return `<span class="text-danger">${esc(it.blockedReason || 'Tidak dapat diterapkan')}</span>`;
        }
    }

    function selectedIds() {
        return $('#giBody .gi-chk:checked').map(function () { return Number($(this).data('id')); }).get();
    }

    function needsReason(ids) {
        return items.some(it => ids.includes(it.matrixId) && it.mode === 'NewCr');
    }

    function refreshControls() {
        const ids = selectedIds();
        $('#giSelCount').text(ids.length);
        $('#giApply').prop('disabled', ids.length === 0);
        $('#giReasonWrap').toggleClass('d-none', !needsReason(ids));

        const enabled = $('#giBody .gi-chk:not(:disabled)').length;
        $('#giChkAll').prop('checked', enabled > 0 && ids.length === enabled);
    }

    /* ---------- render ---------- */

    function render(data) {
        items = data.items || [];

        $('#giName').text(data.groupName);

        $('#giSummary').html(
            `Group berisi <b>${data.activeCourseCount}</b> course aktif. ` +
            `Dipakai <b>${data.totalMatrices}</b> matrix: <b>${data.upToDateMatrices}</b> sudah sinkron, ` +
            `<b>${items.length}</b> perlu tindakan.`);

        const $body = $('#giBody').empty();

        if (items.length === 0) {
            $body.append('<tr><td colspan="7" class="text-center text-muted py-3">Semua matrix sudah sinkron dengan group ini.</td></tr>');
        }

        items.forEach(function (it) {
            const blocked = it.mode === 'Blocked';
            const codes = (it.missingCourses || []).map(c =>
                `<span class="badge bg-light text-dark border me-1" title="${esc(c.courseName)}">${esc(c.courseCode)}</span>`).join('');

            $body.append(
                `<tr>` +
                `<td class="text-center"><input type="checkbox" class="form-check-input gi-chk" data-id="${it.matrixId}" ${blocked || !canEdit ? 'disabled' : ''} /></td>` +
                `<td class="fw-medium">${esc(it.departmentAbrv)}</td>` +
                `<td class="text-center">${statusBadge(it.status)}</td>` +
                `<td class="text-center">${it.cellCount}</td>` +
                `<td class="text-center">${it.missingCount}</td>` +
                `<td class="gi-courses">${codes}</td>` +
                `<td class="small">${modeText(it)}</td>` +
                `</tr>`);
        });

        $('#giApply').toggleClass('d-none', !canEdit || items.length === 0);
        $('#giReason').val('');
        $('#giChkAll').prop('disabled', !canEdit);
        refreshControls();
    }

    /* ---------- load ---------- */

    function load(id) {
        return $.getJSON(urls.getImpact, { groupId: id }).then(function (res) {
            if (!res.success) {
                toast('error', res.message || 'Gagal memuat data.');
                return null;
            }
            return res.data;
        }, function (xhr) { ajaxError(xhr); return null; });
    }

    function open(id, name) {
        current = { id: id, name: name };

        return load(id).then(function (data) {
            if (!data) return;
            render(data);
            modal.show();
        });
    }

    /** Dipanggil setelah Save group: modal dibuka hanya bila memang ada matrix yang perlu tindakan. */
    function openIfNeeded(id, name) {
        current = { id: id, name: name };

        return load(id).then(function (data) {
            if (!data || (data.items || []).length === 0) return;
            render(data);
            modal.show();
        });
    }

    /* ---------- apply ---------- */

    function resultHtml(results) {
        const rows = results.map(function (r) {
            const icon = r.success ? '<span class="text-success">&#10003;</span>' : '<span class="text-danger">&#10007;</span>';
            const dept = r.departmentAbrv ? `<b>${esc(r.departmentAbrv)}</b>: ` : '';
            const link = r.changeRequestId
                ? ` <a href="${urls.crDetail}?id=${r.changeRequestId}" target="_blank">buka CR</a>` : '';
            return `<li>${icon} ${dept}${esc(r.message)}${link}</li>`;
        }).join('');

        return `<ul class="text-start small mb-0" style="list-style:none;padding-left:0">${rows}</ul>`;
    }

    function apply() {
        const ids = selectedIds();
        if (ids.length === 0) return;

        const reason = ($('#giReason').val() || '').trim();

        if (needsReason(ids) && !reason) {
            toast('warning', 'Alasan Change Request wajib diisi.');
            $('#giReason').trigger('focus');
            return;
        }

        const body = new URLSearchParams();
        body.append('__RequestVerificationToken', token());
        body.append('groupId', current.id);
        body.append('reason', reason);
        ids.forEach(id => body.append('matrixIds', id));

        const crCount = items.filter(it => ids.includes(it.matrixId) && it.mode !== 'Direct').length;

        Swal.fire({
            icon: 'question',
            title: 'Terapkan perubahan group?',
            html: `${ids.length} matrix akan diproses` +
                (crCount > 0 ? ` (${crCount} lewat Change Request yang masih perlu Anda submit untuk approval)` : '') + '.',
            showCancelButton: true,
            confirmButtonText: 'Ya',
            cancelButtonText: 'Batal'
        }).then(function (r) {
            if (!r.isConfirmed) return;

            const $btn = $('#giApply').prop('disabled', true);

            $.ajax({
                url: urls.apply,
                method: 'POST',
                data: body.toString(),
                contentType: 'application/x-www-form-urlencoded; charset=UTF-8'
            }).done(function (res) {
                if (!res.success) {
                    toast('error', res.message);
                    return;
                }

                const results = res.data.results || [];
                const failed = results.filter(x => !x.success).length;

                Swal.fire({
                    icon: failed === 0 ? 'success' : 'warning',
                    title: failed === 0 ? 'Selesai' : `Selesai, ${failed} gagal`,
                    html: resultHtml(results),
                    confirmButtonText: 'OK'
                });

                open(current.id, current.name);
            }).fail(ajaxError)
              .always(() => $btn.prop('disabled', false));
        });
    }

    /* ---------- init ---------- */

    $(function () {
        modal = new bootstrap.Modal(document.getElementById('giModal'));

        $('#cgTable').on('click', '.cg-impact', function () {
            open($(this).data('id'), $(this).data('name'));
        });

        $('#giApply').on('click', apply);

        $('#giBody').on('change', '.gi-chk', refreshControls);

        $('#giChkAll').on('change', function () {
            $('#giBody .gi-chk:not(:disabled)').prop('checked', this.checked);
            refreshControls();
        });

        window.groupImpact = { open: open, openIfNeeded: openIfNeeded };
    });

})();
