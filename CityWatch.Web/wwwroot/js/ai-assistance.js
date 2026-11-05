/*
 * ai-assistance.js
 * ---------------------------------------------------------------------------
 * AI Assistance for the incident text field on /Incident/Register.
 *
 * Replaces the two buttons that used to live in site.js (#btnAIButton and
 * #btnAIButtonOpenAIApi). Both of those overwrote #Report_Feedback the moment
 * a response came back - including, when the provider key was missing, with
 * the literal text "OpenAI error". Nothing here touches the field until the
 * guard presses a Use button.
 *
 * Every call goes to a Razor Page handler on this page, so no provider
 * credential is ever in the browser and the handlers inherit the /Incident
 * folder's authorization.
 */
(function () {
    'use strict';

    var TEXT_FIELD = '#Report_Feedback';
    var EMPTY_MESSAGE = 'Text field requires content for AI to scan';

    var state = {
        original: '',       // the guard's text, exactly as typed
        matches: [],        // grammar findings, each with an .applied flag
        result: '',         // rewrite / translation result awaiting review
        languagesLoaded: false
    };

    function token() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    function escapeHtml(value) {
        return $('<div/>').text(value === null || typeof value === 'undefined' ? '' : value).html();
    }

    function showAlert(message) {
        $('#aiAssistanceAlert').html(escapeHtml(message)).show();
    }

    function clearAlert() {
        $('#aiAssistanceAlert').hide().empty();
    }

    /* One place decides which panel and which footer buttons are on screen, so the footer can never
       disagree with the step being shown. */
    function showStep(step) {
        $('#aiAssistanceChoices, #aiAssistanceLanguageStep, #aiAssistanceGrammarStep, #aiAssistanceResultStep').hide();
        $('#aiBack, #aiUseGrammarResult, #aiRunImprove, #aiRunTranslate, #aiUseResult').hide();

        $(step).show();

        if (step === '#aiAssistanceGrammarStep') {
            $('#aiBack, #aiUseGrammarResult, #aiRunImprove').show();
        } else if (step === '#aiAssistanceLanguageStep') {
            $('#aiBack, #aiRunTranslate').show();
        } else if (step === '#aiAssistanceResultStep') {
            $('#aiBack, #aiUseResult').show();
        }
    }

    /* Locks the modal while a request is in flight, so a second click cannot start another one. */
    function setBusy(isBusy, message) {
        $('#aiAssistanceBusyText').text(message || 'Processing with AI...');
        $('#aiAssistanceBusy').toggle(isBusy);
        $('#aiAssistanceModal').find('button').not('#aiAssistanceCancel').prop('disabled', isBusy);
    }

    function post(handler, data, busyMessage) {
        clearAlert();
        setBusy(true, busyMessage);

        return $.ajax({
            url: '/Incident/Register?handler=' + handler,
            type: 'POST',
            data: data,
            headers: { 'RequestVerificationToken': token() }
        }).always(function () {
            setBusy(false);
        });
    }

    /* The Incident Register must keep working when AI does not, so a failure only ever puts a
       message in this modal - it never blocks the form or the save. */
    function fail(message) {
        showAlert(message || 'AI assistance is temporarily unavailable. Please try again.');
    }

    function failFromXhr(xhr) {
        if (xhr && xhr.statusText === 'timeout') {
            fail('The AI request took too long. Please try again.');
            return;
        }
        fail('AI assistance is temporarily unavailable. Please try again.');
    }

    // The single place the incident field is written.
    function applyToField(text) {
        $(TEXT_FIELD).val(text).trigger('change');
        $('#aiAssistanceModal').modal('hide');
    }

    // -----------------------------------------------------------------------
    // Opening
    // -----------------------------------------------------------------------

    $(document).on('click', '#btnAiAssistance', function (e) {
        e.preventDefault();

        var text = $(TEXT_FIELD).val() || '';

        // Whitespace-only counts as empty, and no request is made for it.
        if (!text.trim()) {
            new MessageModal({ title: 'AI Assistance', message: EMPTY_MESSAGE }).showWarning();
            return;
        }

        state.original = text;
        state.matches = [];
        state.result = '';

        clearAlert();
        setBusy(false);
        showStep('#aiAssistanceChoices');

        $('#aiAssistanceModal').modal('show');
    });

    // -----------------------------------------------------------------------
    // Grammar & Scenario Check
    // -----------------------------------------------------------------------

    $(document).on('click', '#aiChoiceGrammar', function () {
        post('AiGrammarCheck', { Text: state.original }, 'Checking grammar...')
            .done(function (response) {
                if (!response || !response.success) {
                    fail(response && response.message);
                    return;
                }
                renderGrammar(response.result);
                showStep('#aiAssistanceGrammarStep');
            })
            .fail(failFromXhr);
    });

    function renderGrammar(result) {
        $('#aiGrammarLanguage').text(result.detectedLanguage || 'Unknown');
        $('#aiGrammarOriginal').text(result.originalText);

        // Only the findings that can actually change something are selectable.
        state.matches = (result.matches || [])
            .filter(function (match) { return match.replacements && match.replacements.length > 0; })
            .map(function (match) {
                match.applied = true;   // suggestions start accepted; the guard can untick any of them
                return match;
            });

        var unusable = (result.matches || []).length - state.matches.length;
        var list = $('#aiGrammarSuggestions').empty();

        if (state.matches.length === 0) {
            list.append('<li class="list-group-item text-success" style="font-size:12px;padding:4px">' +
                'No spelling or grammar corrections were suggested.</li>');
            $('#aiUseGrammarResult').prop('disabled', true);
        } else {
            $('#aiUseGrammarResult').prop('disabled', false);

            state.matches.forEach(function (match, index) {
                list.append(
                    '<li class="list-group-item ai-suggestion ai-suggestion-applied" data-index="' + index + '"' +
                    ' style="font-size:12px;padding:4px">' +
                    '<i class="fa fa-check text-success mr-2 ai-suggestion-tick" aria-hidden="true"></i>' +
                    '"' + escapeHtml(match.original) + '" &rarr; ' +
                    '<strong class="ai-suggestion-replacement">' + escapeHtml(match.replacements[0]) + '</strong>' +
                    '<div class="text-muted">' + escapeHtml(match.message || '') + '</div>' +
                    '</li>');
            });
        }

        if (unusable > 0) {
            list.append('<li class="list-group-item text-muted" style="font-size:12px;padding:4px">' +
                unusable + ' further issue(s) were flagged with no suggested correction.</li>');
        }

        renderCorrected();
    }

    // Ticking is per suggestion, so the guard decides each correction rather than taking all or none.
    $(document).on('click', '.ai-suggestion', function () {
        var index = $(this).data('index');
        if (typeof index === 'undefined') return;

        var applied = !state.matches[index].applied;
        state.matches[index].applied = applied;

        $(this).toggleClass('ai-suggestion-applied', applied)
               .toggleClass('ai-suggestion-ignored', !applied);

        renderCorrected();
    });

    /*
     * Rebuilds the corrected text from the accepted suggestions only.
     *
     * Works on the guard's original string and walks the matches from the end backwards, so earlier
     * offsets stay valid without tracking a running adjustment, and so every character the guard
     * typed between matches - line breaks, blank lines, indentation, bullet characters - survives
     * untouched. Overlapping matches are skipped rather than corrupting the text.
     */
    function buildCorrectedText() {
        var accepted = state.matches
            .filter(function (match) { return match.applied; })
            .sort(function (a, b) { return b.offset - a.offset; });

        var text = state.original;
        var nextStart = state.original.length;

        accepted.forEach(function (match) {
            if (match.offset + match.length > nextStart) return;   // overlaps the one already applied

            text = text.substring(0, match.offset)
                 + match.replacements[0]
                 + text.substring(match.offset + match.length);

            nextStart = match.offset;
        });

        return text;
    }

    function renderCorrected() {
        $('#aiGrammarCorrected').text(buildCorrectedText());
    }

    // Applies the ticked corrections straight to the field - no second review screen.
    $(document).on('click', '#aiUseGrammarResult', function () {
        applyToField(buildCorrectedText());
    });

    $(document).on('click', '#aiRunImprove', function () {
        post('AiImproveIncident', { Text: state.original }, 'Processing with AI...')
            .done(function (response) {
                if (!response || !response.success) {
                    fail(response && response.message);
                    return;
                }
                state.result = response.result.resultText;
                renderResult();
                showStep('#aiAssistanceResultStep');
            })
            .fail(failFromXhr);
    });

    // -----------------------------------------------------------------------
    // Language Converter
    // -----------------------------------------------------------------------

    $(document).on('click', '#aiChoiceTranslate', function () {
        // The source language is detected, never chosen by the guard.
        $('#aiDetectedLanguage').text('detecting...');
        showStep('#aiAssistanceLanguageStep');

        loadLanguages();

        // Reuses the grammar check purely for its language detection.
        post('AiGrammarCheck', { Text: state.original }, 'Detecting language...')
            .done(function (response) {
                $('#aiDetectedLanguage').text(
                    (response && response.success && response.result.detectedLanguage) || 'Unknown');
            })
            .fail(function () {
                $('#aiDetectedLanguage').text('Unknown');
            });
    });

    /* The list comes from the server so it can be changed in configuration rather than in this file. */
    function loadLanguages() {
        if (state.languagesLoaded) return;

        $.ajax({
            url: '/Incident/Register?handler=AiLanguages',
            type: 'GET',
            cache: false
        }).done(function (response) {
            if (!response || !response.success) return;

            var select = $('#aiTargetLanguage').empty();
            response.languages.forEach(function (language) {
                select.append($('<option/>').val(language.code).text(language.name));
            });
            state.languagesLoaded = true;
        }).fail(function () {
            showAlert('The language list could not be loaded. Please try again.');
        });
    }

    $(document).on('click', '#aiRunTranslate', function () {
        var target = $('#aiTargetLanguage').val();

        if (!target) {
            showAlert('The selected language is not supported.');
            return;
        }

        post('AiTranslate', { Text: state.original, TargetLanguage: target }, 'Translating text...')
            .done(function (response) {
                if (!response || !response.success) {
                    fail(response && response.message);
                    return;
                }
                state.result = response.result.resultText;
                renderResult();
                showStep('#aiAssistanceResultStep');
            })
            .fail(failFromXhr);
    });

    // -----------------------------------------------------------------------
    // Review - for the rewrite and the conversion, which replace the whole text
    // -----------------------------------------------------------------------

    function renderResult() {
        $('#aiResultOriginal').text(state.original);
        $('#aiResultText').text(state.result);
    }

    $(document).on('click', '#aiUseResult', function () {
        if (!state.result) return;
        applyToField(state.result);
    });

    $(document).on('click', '#aiBack', function () {
        clearAlert();
        showStep('#aiAssistanceChoices');
    });

    // Leave nothing from the last run on screen for the next one.
    $(document).on('hidden.bs.modal', '#aiAssistanceModal', function () {
        state.original = '';
        state.matches = [];
        state.result = '';
        clearAlert();
        setBusy(false);
        showStep('#aiAssistanceChoices');
    });
})();
