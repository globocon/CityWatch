$(function () {
    $('#Report_DateLocation_ClientType').on('change', function () {
        $('#Report_DateLocation_ClientSite').val('');
        $('#Report_DateLocation_ClientAddress').val('');
        toggleClientGpsLink(false);

        const ulClients = $('#Report_DateLocation_ClientSite').siblings('ul.es-list');
        ulClients.html('');

        const option = $(this).val();
        if (option == '')
            return false;

        $.ajax({
            url: '/Incident/Register?handler=ClientSites&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    ulClients.append('<li class="es-visible" value="' + site.value + '">' + site.text + '</li>');
                });
            }
        });
    });

    $('#Report_DateLocation_ClientSite').attr('placeholder', 'Select Site or Edit');
    $('#Report_DateLocation_ClientSite').editableSelect({
        //filter: false,
        effects: 'slide'
    }).on('select.editable-select', function (e, li) {
        $.ajax({
            url: '/Incident/Register?handler=ClientSiteByName',
            type: 'GET',
            data: { name: li.text() }
        }).done(function (data) {
            if (data.success) {
                $('#Report_DateLocation_ClientAddress').val(data.clientSite.address);
                $('#Report_DateLocation_State').val(data.clientSite.state);
                $('#Report_Officer_Billing').val(data.clientSite.billing);
                if (data.clientSite.gps) toggleClientGpsLink(true, data.clientSite.gps);
                else toggleClientGpsLink(false);
                setSelectedClientStatus(data.clientSite);
                $('#Report_DateLocation_ShowIncidentLocationAddress').prop('checked', false);
                $('#clientSiteAddress').val(data.clientSite.address);
                $('#clientSiteGps').val(data.clientSite.gps);
            }
        }).fail(function () {
        });
    });

    // Only to handle the clear event of client site input
    $('#Report_DateLocation_ClientSite').on('change', function () {
        if ($(this).val() === '') {
            $('#Report_DateLocation_ClientAddress').val('');
            toggleClientGpsLink(false);
        }
    });

    function setSelectedClientStatus(clientSite) {
        $('#client_status_0').removeClass('text-success');
        $('#client_status_1').removeClass('text-warning');
        $('#client_status_2').removeClass('text-danger');
        $('#client_status_date_1').text('');
        $('#client_status_date_2').text('');
        $('#client_site_expiring_msg').removeClass('d-block').addClass('d-none');

        let status = clientSite.status;
        if (status === 1 && new Date(clientSite.statusDate) < new Date())
            status = 2;
        $('#client_status_' + status).addClass(clientSiteStatuses.getColorByValue(status));

        const calIcon = '<i class="fa fa-calendar mx-2"></i>'
        if (status === 1) {
            $('#client_status_date_1').html(calIcon + clientSite.formattedStatusDate);
        }
        else if (status === 2) {
            $('#client_site_expiring_msg').addClass('d-block');
            $('#client_status_date_2').html(calIcon + clientSite.formattedStatusDate);
        }
    }

    $('#Report_Officer_NotifiedBy').editableSelect({
        effects: 'slide'
    });

    $('#Report_Officer_CallSign').editableSelect({
        effects: 'slide'
    });



    // editable dropbox issue select the first itesm by deafult Start 
    const urlParams = new URLSearchParams(window.location.search);
    const myParam = urlParams.get('reuse');
    if (myParam === null) {
        //if the resuse IR not clicked 
        $('#Report_Officer_NotifiedBy').val('');
        $('#Report_Officer_CallSign').val('');

    }
    var uls = document.getElementsByClassName('es-list');
    // Loop through each <ul> element
    for (var i = 0; i < uls.length; i++) {
        var ul = uls[i];

        // Loop through each <li> element within the current <ul>
        ul.querySelectorAll('li').forEach(function (li) {
            // Retrieve and log the text content of each <li>
            li.removeAttribute('style');
            li.classList.add('es-visible');

        });
    }
    // editable dropbox select by deafult end

    $('#Report_DateLocation_ClientArea').attr('placeholder', 'n/a')

    $('#ddFeedbackTemplateType').on('change', function (e, colorCode) {
        const type = $(this).val();
        if (type == '')
            return false;

        $('#ddFeedbackTemplate').html('');

        $.ajax({
            url: '/Incident/Register?handler=FeedbackTemplatesByType&type=' + type,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            data.map(function (feedback) {
                $('#ddFeedbackTemplate').append('<option value="' + feedback.value + '">' + feedback.text + '</option>');
            });

            if (colorCode) {
                $('#ddFeedbackTemplate').val(colorCode);
                $('#ddFeedbackTemplate').trigger('change');
            }
        });
    });

    $('#ddFeedbackTemplate').on('change', function () {
        const option = $(this).val();
        $('#Report_Feedback').val('');
        if (option == '')
            return false;

        $.ajax({
            url: '/Incident/Register?handler=FeedbackTemplate&templateId=' + option,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $('#Report_Feedback').val(data.text);
            }
        });
    });

    $('#Report_SiteColourCodeId').on('change', function () {
        const option = $(this).val();
        const optionText = $("option:selected", this).text();
        var filter = "Colour Codes";
        $('#Report_Feedback').val('');
        if (option == '')
            return false;

        // Set feedback Template Type to 3 = 'Colour Codes'
        /*to get the colour code and its id into feedbacktype dropdown-start*/
        $.ajax({
            url: '/Incident/Register?handler=FeedbackTypesId&filter=' + filter,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#ddFeedbackTemplateType').val(data.truckConfigText);
            $('#ddFeedbackTemplateType').trigger('change', [option]);
            $('#Report_SiteColourCode').val(optionText);
        });
        /*to get the colour code and its id into feedbacktype dropdown-end*/
    });

    $('#positionfilter').on('change', function () {
        const isChecked = $(this).is(':checked');
        const filter = isChecked ? 1 : 2;
        $.ajax({
            url: '/Incident/Register?handler=OfficerPositions&filter=' + filter,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#Report_Officer_Position').html('');
            data.map(function (position) {
                $('#Report_Officer_Position').append('<option value="' + position.value + '">' + position.text + '</option>');
                
            });
        });
    });
    $('#Report_Officer_Position').on('change', function () {
        var chechedvalue = $('#positionfilter').is(':checked');
        const filtercheck = chechedvalue ? 1 : 2;
        if (filtercheck==1) {
            var logbbokName = $('#Report_Officer_Position').val();
            const token = $('input[name="__RequestVerificationToken"]').val();
            //To get the IsLogbook data from db start
            $.ajax({
                url: '/Incident/Register?handler=LogbookData',
                data: { logbook: logbbokName },
                type: 'GET',
                dataType: 'json',
                headers: { 'RequestVerificationToken': token },
            }).done(function (data) {
               
                if (data.isLogbook == true) {
                    $('#PositionModel').modal('show');
                }
                
            });
                //To get the IsLogbook data from db stop
        }
        
                });
                
    $('#Report_DateLocation_ClientTypePosition').on('change', function () {
        $('#Report_DateLocation_ClientSitePosition').val('');
        //$('#Report_DateLocation_ClientAddress').val('');
       
        const ulClientsPosition = $('#Report_DateLocation_ClientSitePosition').siblings('ul.es-list');
        ulClientsPosition.html('');

        const option = $(this).val();
        if (option == '')
            return false;

        $.ajax({
            url: '/Incident/Register?handler=ClientSites&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    ulClientsPosition.append('<li class="es-visible" value="' + site.value + '">' + site.text + '</li>');
                });
            }
        });
    });
    $('#Report_DateLocation_ClientSitePosition').attr('placeholder', 'Select Site');
    $('#Report_DateLocation_ClientSitePosition').editableSelect({
        //filter: false,
        effects: 'slide'
    });
    /****** Client Type & Client Site Settings *******/
    let gridType,
        gridSite;

    gridType = $('#client_type_settings').grid({
        dataSource: '/Admin/Settings?handler=ClientTypes',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'name', title: 'Client Type', width: 400, editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridType) {
        gridType.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=ClientTypes',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridType.reload();

                $('#sel_client_type').html('');
                $.ajax({
                    url: '/Admin/Settings?handler=ClientTypes',
                    type: 'GET',
                    dataType: 'json'
                }).done(function (data) {
                    $('#sel_client_type').append('<option value="">All</option>');
                    data.map(function (clientType) {
                        $('#sel_client_type').append('<option value="' + clientType.id + '">' + clientType.name + '</option>');
                    });
                    gridSite.reload();
                });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isClientTypeAdding)
                    isClientTypeAdding = false;
            });
        });

        gridType.on('rowRemoving', function (e, id, record) {
            const isAdminLoggedIn = $('#hdnIsAdminLoggedIn').val();
            if (isAdminLoggedIn === 'False') {
                showModal('Insufficient permission to perform this operation');
                return;
            }

            if (confirm('Are you sure want to delete this client type and its related client sites?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteClientType',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function (respose) {
                    if (respose.status == false) {
                        alert(respose.message);
                    }
                    else {
                        gridType.reload();

                        $('#sel_client_type').html('');
                        $.ajax({
                            url: '/Admin/Settings?handler=ClientTypes',
                            type: 'GET',
                            dataType: 'json'
                        }).done(function (data) {
                            $('#sel_client_type').append('<option value="">All</option>');
                            data.map(function (clientType) {
                                $('#sel_client_type').append('<option value="' + clientType.id + '">' + clientType.name + '</option>');
                            });
                            gridSite.reload();
                        });
                    }
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isClientTypeAdding)
                        isClientTypeAdding = false;
                });
            }
        });
    }

    let isClientTypeAdding = false;
    $('#add_client_type').on('click', function () {

        if (isClientTypeAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isClientTypeAdding = true;
            gridType.addRow({ 'id': -1, 'name': '' }).edit(-1);
        }
    });

    function gpsEditor($editorContainer, value, record) {
        if (!value) value = '';
        const clientGps = $('<input type="text" readonly class="bg-light" id="client_gps_' + record.id + '" value="' + value + '">');
        $editorContainer.append(clientGps);
    }

    function gpsRenderer(value, record) {
        if (value && value !== '') {
            return getGpsAsHyperLink(value);
        }

        return value;
    }

    function addressEditor($editorContainer, value, record) {
        const clientAddress = $('<input type="text" id="client_address_' + record.id + '" value="' + value + '">');
        $editorContainer.append(clientAddress);

        const autoComplete = new google.maps.places.Autocomplete(document.getElementById('client_address_' + record.id), {
            types: ['address'],
            componentRestrictions: { country: 'AU' },
            fields: ['place_id', 'geometry', 'name']
        });

        autoComplete.addListener('place_changed', function () {
            const place = this.getPlace();
            if (place.geometry) {
                var lat = place.geometry.location.lat();
                var lon = place.geometry.location.lng();
                document.getElementById('client_gps_' + record.id).value = lat + ',' + lon;
            }
        });
    }

    gridSite = $('#client_site_settings').grid({
        dataSource: '/Admin/Settings?handler=ClientSites',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'typeId', hidden: true },
            { field: 'clientType', title: 'Client Type', width: 180, renderer: function (value, record) { return value.name; } },
            { field: 'name', title: 'Client Site', width: 180, editor: true },
            { field: 'emails', title: 'Emails', width: 200, editor: true },
            { field: 'address', title: 'Address', width: 200, editor: addressEditor },
            { field: 'state', title: 'State', width: 80, type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=ClientStates', valueField: 'name', textField: 'name' } },
            { field: 'gps', title: 'GPS', width: 100, editor: gpsEditor, renderer: gpsRenderer },
            { field: 'billing', title: 'Billing', width: 100, editor: true },
            { field: 'status', title: 'Status', width: 150, renderer: statusTypeRenderer, editor: statusTypeEditor },
            { field: 'statusDate', hidden: true, editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridSite) {
        gridSite.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            data.status = !Number.isInteger(data.status) ? clientSiteStatuses.getValue(data.status) : data.status;
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=ClientSites',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridSite.clear();
                gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isClientSiteAdding)
                    isClientSiteAdding = false;
            });
        });

        gridSite.on('rowRemoving', function (e, id, record) {
            const isAdminLoggedIn = $('#hdnIsAdminLoggedIn').val();
            if (isAdminLoggedIn === 'False') {
                showModal('Insufficient permission to perform this operation');
                return;
            }

            if (confirm('Are you sure want to delete this client site?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteClientSite',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridSite.reload();
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isClientSiteAdding)
                        isClientSiteAdding = false;
                });
            }
        });
    }

    let isClientSiteAdding = false;
    $('#add_client_site').on('click', function () {
        const selTypeId = $('#sel_client_type').val();
        if (!selTypeId) {
            alert('Select a Client Type');
            return;
        }

        if (isClientSiteAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isClientSiteAdding = true;
            gridSite.addRow({
                'id': -1,
                'name': '',
                'typeId': selTypeId,
                'clientType': {
                    'id': selTypeId,
                    'name': $('#sel_client_type option:selected').text()
                }
            }).edit(-1);
        }
    });

    /** Client Site Status & Status Date **/
    const clientSiteStatuses = {
        data: [
            { id: 0, text: 'Ongoing', color: 'text-success' },
            { id: 1, text: 'Expiring', color: 'text-warning' },
            { id: 2, text: 'Expired', color: 'text-danger' }
        ],
        getText: function (value) {
            const item = this.data.find((e) => e.id === value);
            return item ? item.text : '';
        },
        getValue: function (text) {
            const item = this.data.find((e) => e.text === text);
            return item ? item.id : -1;
        },
        getColorByText: function (text) {
            const item = this.data.find((e) => e.text === text);
            return item ? item.color : '';
        },
        getColorByValue: function (value) {
            const item = this.data.find((e) => e.id === value);
            return item ? item.color : '';
        }
    }

    $('#client_site_settings').on('change', '.dd-client-status', function () {
        if (clientSiteStatuses.getValue($(this).find('option:selected').text()) > 0)
            $(this).next('input').show();
        else
            $(this).next('input').hide();
    });

    $('#client_site_settings').on('change', '.dt-client-statusdate', function () {
        $(this).closest('td').next('td').find('input').val($(this).val());

        const recordId = $(this).attr("data-id");
        var data = gridSite.getById(recordId);
        data.statusDate = $(this).val();
    });

    function statusTypeRenderer(value, record) {
        let rendering = false;
        if (Number.isInteger(value)) rendering = true;
        else if (value === undefined) value = 0;
        else {
            value = clientSiteStatuses.getValue(value);
            if (value === -1) value = 0;
        }

        if (value === 1 && record.formattedStatusDate && new Date(record.formattedStatusDate) < new Date())
            value = 2;

        const statusText = clientSiteStatuses.getText(value);
        const color = clientSiteStatuses.getColorByText(statusText);
        let statusDate = '';
        if (value !== 0) {
            if (rendering) {
                statusDate = record.formattedStatusDate;
            } else if (record.statusDate) {
                statusDate = new Intl.DateTimeFormat('en-Au', { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(record.statusDate));
            }
        }
        return '<div><i class="fa fa-circle ' + color + ' mr-2"></i>' + statusText + '</div><div>' + statusDate + '</div>';
    }

    function statusTypeEditor($editorContainer, value, record) {
        if (isClientSiteAdding && value === undefined)
            value = 0;
        let selectHtml = $('<select class="form-control dd-client-status"></select>');
        clientSiteStatuses.data.forEach(function (item) {
            const selected = item.id === value ? 'selected' : '';
            selectHtml.append('<option "value="' + item.id + '" ' + selected + '>' + item.text + '</option>')
        });
        $editorContainer.append(selectHtml);
        const showDate = value === 0 ? 'display:none' : 'display:block';
        const datePikcer = $('<input type="date" class="dt-client-statusdate form-control" data-id="' + record.id + '" style="' + showDate + '" value="' + record.statusDate.split('T')[0] + '">')
        $editorContainer.append(datePikcer);
    }

    $('#sel_client_type').on('change', function () {
        gridSite.reload({ typeId: $(this).val() });
    });

    $('#search_kw_client_site').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $(this).val() });
        }
    });

    $('#btnSearchClientSite').on('click', function () {
        gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val() });
    });

    /****** Report Fileds start *******/
    let gridReportFields;

    gridReportFields = $('#field_settings').grid({
        dataSource: '/Admin/Settings?handler=ReportFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'name', title: 'Name', width: 200, editor: true },
            { field: 'emailTo', title: 'Special Email Condition', width: 350, editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    let isReportFieldAdding = false;
    $('#add_field_settings').on('click', function () {
        const selFieldTypeId = $('#report_field_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }

        if (isReportFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isReportFieldAdding = true;
            gridReportFields.addRow({
                'id': -1,
                'typeId': selFieldTypeId,
                'name': '',
                'emailTo': ''
            }).edit(-1);
        }
    });

    $('#report_field_types').on('change', function () {
        const selFieldTypeId = $(this).val();

        if (!selFieldTypeId) { // None
            $('#fieldSettings').show();
            $('#positionSettings').hide();

            gridReportFields.clear();
            gridPositions.clear();
            gridReportFields.reload({ typeId: selFieldTypeId });

        } else if (selFieldTypeId === '1') { // Position
            $('#fieldSettings').hide();
            $('#positionSettings').show();
            $('#PSPFSettings').hide();

            gridReportFields.clear();
            gridPositions.reload();
            gridPSPF.clear();
        }
        else if (selFieldTypeId === '5') {
            $('#PSPFSettings').show();
            $('#fieldSettings').hide();
            $('#positionSettings').hide();

            gridPositions.clear();
            gridReportFields.clear();
            gridPSPF.reload();
        }
        else {
            $('#fieldSettings').show();
            $('#positionSettings').hide();
            $('#PSPFSettings').hide();

            gridPSPF.clear();
            gridPositions.clear();
            gridReportFields.reload({ typeId: selFieldTypeId });
        }
    });

    if (gridReportFields) {
        gridReportFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=ReportField',
                data: { reportfield: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridReportFields.clear();
                gridReportFields.reload({ typeId: $('#report_field_types').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isReportFieldAdding)
                    isReportFieldAdding = false;
            });
        });

        gridReportFields.on('rowRemoving', function (e, id, record) {

            if (confirm('Are you sure want to delete this field?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteReportField',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridReportFields.reload({ typeId: $('#report_field_types').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isReportFieldAdding)
                        isReportFieldAdding = false;
                });
            }
        });
    }
    /****** Report Fileds end *******/

    /****** Report tools start *******/
    let gridReportTools;

    gridReportTools = $('#tools_settings').grid({
        dataSource: '/Admin/Settings?handler=LinksPageDetails',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'title', title: 'Title', width: 200, editor: true },
            { field: 'hyperlink', title: 'Hyperlink', width: 350, editor: true }

        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });




    let isReportTooolsAdding = false;
    $('#add_tools_settings').on('click', function () {
        const selToolsTypeId = $('#report_tools_types').val();
        if (!selToolsTypeId) {
            alert('Please select a button to update');
            return;
        }

        if (isReportTooolsAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isReportTooolsAdding = true;
            gridReportTools.addRow({
                'id': -1,
                'typeId': selToolsTypeId,
                'name': '',
                'emailTo': ''
            }).edit(-1);
        }
    });

    $('#report_tools_types').on('change', function () {
        const selToolsTypeId = $(this).val();

        if (!selToolsTypeId) { // None
            $('#toolsSettings').show();
            gridReportTools.clear();
            gridReportTools.reload({ typeId: selToolsTypeId });

        } else {
            $('#toolsSettings').show();
            gridReportTools.clear();
            gridReportTools.reload({ typeId: selToolsTypeId });
        }
    });

    if (gridReportTools) {
        gridReportTools.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=LinksPageDetails',
                data: { reportfield: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (response) {
                if (!response.status) {
                    alert(response.message);
                }
                gridReportTools.clear();
                gridReportTools.reload({ typeId: $('#report_tools_types').val() });
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isReportTooolsAdding)
                    isReportTooolsAdding = false;
            });
        });

        gridReportTools.on('rowRemoving', function (e, id, record) {

            if (confirm('Are you sure want to delete this field?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeleteLinksPageDetails',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridReportTools.reload({ typeId: $('#report_tools_types').val() });
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isReportTooolsAdding)
                        isReportTooolsAdding = false;
                });
            }
        });
    }

    $('#add_tools_page').on('click', function () {
        $('#pageType').val('');
        $('#tools-modal').modal();
    });
    /*to add the feedback type*/
    $('#add_feedbacktype_page').on('click', function (e) {
        e.preventDefault();
        $('#feedBackType').val('');
        $('#category-modal').modal();
    });




    $('#btnSavePageType').on('click', function () {
        if (newpageTypeIsValid()) {
            var newItem = $("#pageType").val();
            var data = {
                'PageTypeName': $('#pageType').val()
            };
            $.ajax({
                url: '/Admin/Settings?handler=LinksPageType',
                data: { ClientSiteLinksPageTyperecord: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.status == -1) {
                    $('#pageType').val('');
                    $('#pageType-modal-validation').html(data.message).show().delay(2000).fadeOut();
                } else {

                    const button_id = 'attach_' + data.status;
                    const li = document.createElement('li');
                    li.id = button_id;
                    li.className = 'list-group-item';
                    li.dataset.index = data.status;
                    li.style = "border-left: 0;border-right: 0;"
                    let liText = document.createTextNode(newItem);

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-tools-type';
                    icon.title = 'Delete';
                    icon.style = 'cursor: pointer;float:right';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('itemList').append(li);

                    $("#itemInput").val("");
                    // Append the new item to the list

                    $('#pageType').val('');
                    refreshPageType();

                }
            }).fail(function () {
                console.log('error');
            }).always(function () {
            });
        }
    });
    function newpageTypeIsValid() {
        const pageType = $('#pageType').val();
        if (pageType === '') {
            $('#pageType-modal-validation').html('Button name is required').show().delay(2000).fadeOut();
            return false;
        }
        return true;
    }

    $('#btnSaveFeedBackType').on('click', function () {
        if (newfeedbackTypeIsValid()) {
            var newItem = $("#feedBackType").val();
            var data = {
                'Name': $('#feedBackType').val()
            };
            $.ajax({
                url: '/Admin/Settings?handler=FeedBackType',
                data: { FeedbackNewTyperecord: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.status == -1) {
                    $('#feedBackType').val('');
                    $('#feedbackType-modal-validation').html(data.message).show().delay(2000).fadeOut();
                } else {

                    const button_id = 'attach_' + data.status;
                    const li = document.createElement('li');
                    li.id = button_id;
                    li.className = 'list-group-item';
                    li.dataset.index = data.status;
                    li.style = "border-left: 0;border-right: 0;"
                    let liText = document.createTextNode(newItem);

                    const icon = document.createElement("i");
                    icon.className = 'fa fa-trash-o ml-2 text-danger btn-delete-feedback-type';
                    icon.title = 'Delete';
                    icon.style = 'cursor: pointer;float:right';

                    li.appendChild(liText);
                    li.appendChild(icon);
                    document.getElementById('itemfeedbackList').append(li);

                    $("#itemInput").val("");
                    // Append the new item to the list

                    $('#feedBackType').val('');
                    refreshFeedBackType();

                }
            }).fail(function () {
                console.log('error');
            }).always(function () {
            });
        }
    });
    function newfeedbackTypeIsValid() {
        const feedBackType = $('#feedBackType').val();
        if (feedBackType === '') {
            $('#feedBackType-modal-validation').html('Category name is required').show().delay(2000).fadeOut();
            return false;
        }
        return true;
    }

    const refreshPageType = function () {
        $.ajax({
            url: '/Admin/Settings?handler=LinksPageTypeList',
            type: 'GET',
            success: function (data) {
                if (data) {
                    $('#report_tools_types').html('');
                    $('#report_tools_types').append('<option value="">None</option>');
                    data.map(function (template) {
                        $('#report_tools_types').append('<option value="' + template.id + '">' + template.pageTypeName + '</option>');
                    });
                }
            }
        });
    }


    const refreshFeedBackType = function () {
        $.ajax({
            url: '/Admin/Settings?handler=FeedBackTypeList',
            type: 'GET',
            success: function (data) {
                if (data) {
                    $('#FeedbackTemplate_Type').html('');

                    data.map(function (template) {
                        $('#FeedbackTemplate_Type').append('<option value="' + template.id + '">' + template.name + '</option>');
                    });
                }
            }
        });
    }


    var queryString = window.location.search;
    // Parse the query string into an object
    var params = new URLSearchParams(queryString);
    // Get the value of the 'name' parameter
    var type = params.get('type');
    var state = params.get('st');
    let gridTools;
    gridTools = $('#file_Tools').grid({
        /* dataSource: '/Admin/Settings?handler=StaffDocs',*/
        dataSource: '/Admin/Settings?handler=LinkDetailsUisngTypeandState&&type=' + type,
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'title', title: 'Tools', width: 400 },
            { title: 'Weblink', width: 50, renderer: linkClickRenderer },
        ]
    });
    function linkClickRenderer(value, record) {
        if (!/^https?:\/\//i.test(record.hyperlink)) {
            record.hyperlink = 'http://' + record.hyperlink;
        }

        return '<div class="centerIcon"><a href="' + record.hyperlink + '" target="_blank"><img src="../images/Blue_globe_icon.svg" class="imgIcon"/></a></div>'
    }
    $('#itemfeedbackList').on('click', '.btn-delete-feedback-type', function (event) {
        if (confirm('Are you sure want to delete this Category ?')) {
            var target = event.target;
            const fileName = target.parentNode.innerText.trim();
            var itemToDelete = target.parentNode.dataset.index;
            $.ajax({
                url: '/Admin/Settings?handler=DeleteFeedBackType',
                type: 'POST',
                dataType: 'json',
                data: { TypeId: itemToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result) {
                    $('#feedbackType').val('');
                    refreshFeedBackType();
                    target.parentNode.parentNode.removeChild(target.parentNode);

                }
            });
        }
    });

    /* code added for PSPF subfields start*/


    $('#add_PSPF_settings').on('click', function () {
        if (isPSPFAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isPSPFAdding = true;
            gridPSPF.addRow({
                'id': -1,
                'ReferenceNo': '',
                'name': '',
                'isDefault': ''
            }).edit(-1);
        }
    });
    let gridPSPF;

    gridPSPF = $('#PSPF_settings').grid({
        dataSource: '/Admin/Settings?handler=PSPF',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'referenceNo', title: 'Reference No', width: 200, editor: false },
            { field: 'name', title: 'Name', width: 250, editor: true },
            { field: 'isDefault', title: 'Default', type: 'checkbox', align: 'center', width: 100, editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    let isPSPFAdding = false;

    if (gridPSPF) {
        gridPSPF.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=SavePSPF',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridPSPF.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isPSPFAdding)
                    isPSPFAdding = false;
            });
        });

        gridPSPF.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeletePSPF',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridPSPF.reload();
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isPSPFAdding)
                        isPSPFAdding = false;
                });
            }
        });
    }

    /* code added for PSPF subfields stop*/

    /****** Report tools end *******/

    let gridPositions;

    gridPositions = $('#position_settings').grid({
        dataSource: '/Admin/Settings?handler=Positions',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'name', title: 'Name', width: 250, editor: true },
            { field: 'emailTo', title: 'Special Email Condition', width: 200, editor: true },
            { field: 'isPatrolCar', title: 'Patrol Car?', type: 'checkbox', align: 'center', width: 100, editor: true },
            { field: 'dropboxDir', title: 'Dropbox Directory', width: 300, editor: true },
            { field: 'isLogbook', title: 'Logbook', type: 'checkbox', align: 'center', width: 100, editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridPositions) {
        gridPositions.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=SavePositions',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
                gridPositions.reload();
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isPositionAdding)
                    isPositionAdding = false;
            });
        });

        gridPositions.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                const token = $('input[name="__RequestVerificationToken"]').val();
                $.ajax({
                    url: '/Admin/Settings?handler=DeletePosition',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': token },
                }).done(function () {
                    gridPositions.reload();
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isPositionAdding)
                        isPositionAdding = false;
                });
            }
        });
    }

    let isPositionAdding = false;
    $('#add_position_settings').on('click', function () {
        if (isPositionAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isPositionAdding = true;
            gridPositions.addRow({
                'id': -1,
                'name': '',
                'emailTo': '',
                'isPatrolCar': false,
                'dropboxDir': ''
            }).edit(-1);
        }
    });

    /****** User & User Client Access *******/

    let gridUsers,
        gridClientSiteAccess,
        ucaTree;

    $('#user_settings').on('click', '.deleteuser', function () {
        if (confirm('Are you sure want to delete this user?')) {
            toggleUserStatus($(this).attr('data-user-id'), true);
        }
    });

    $('#user_settings').on('click', '.activateuser', function () {
        toggleUserStatus($(this).attr('data-user-id'), false);
    });

    function toggleUserStatus(userId, deleted) {
        $.ajax({
            url: '/Admin/Settings?handler=UpdateUserStatus',
            data: { id: userId, deleted: deleted },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridUsers.reload();
            gridClientSiteAccess.reload();
        }).fail(function () {
            console.log('error')
        });
    }

    gridUsers = $('#user_settings').grid({
        dataSource: '/Admin/Settings?handler=Users',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'userName', title: 'User Name', width: 200 },
            { title: 'Password', width: 200, renderer: passwordRenderer },
            { field: 'isDeleted', title: 'Deleted?', align: 'center', width: 50, renderer: function (value) { return value ? 'Yes' : '&nbsp;'; } },
            { width: 150, renderer: userButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    $('#user_settings').on('click', ".showPassword", function () {
        const btn = $(this);
        const userId = btn.attr('data-uid');
        const spanElem = btn.siblings().first();
        $.ajax({
            url: '/Admin/Settings?handler=ShowPassword',
            data: { id: userId },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            spanElem.text(data);
            btn.hide();
        }).fail(function () {
            console.log('error')
        });
    });

    function passwordRenderer(value, record) {
        return '<span id="user_password_' + record.id + '"></span><button class="btn btn-light showPassword" data-uid="' + record.id + '"><i class="fa fa-eye mr-2"></i>Show</button>';
    }

    function userButtonRenderer(value, record) {
        let userButtonHtml = '<button class="btn btn-outline-primary mr-2" data-toggle="modal" data-target="#user-modal" data-id="' + record.id + '" data-uname="' + record.userName + '"' +
            'data-udeleted="' + record.isDeleted + '" data-action="editUser"><i class="fa fa-pencil mr-2"></i>Edit</button>';

        if (record.isDeleted) {
            userButtonHtml += '<button class="btn btn-outline-success activateuser" data-user-id="' + record.id + '""> <i class="fa fa-check mr-2" aria-hidden="true"></i>Activate</button>';
        } else {
            userButtonHtml += '<button class="btn btn-outline-danger deleteuser" data-user-id="' + record.id + '""> <i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
        }

        return userButtonHtml;
    }

    $('#add_user').on('click', function () {
        $('#userId').val('');
        $('#userName').val('');
        $('#userName').prop('readonly', false);
        $('#userPassword').val('');
        $('#userConfirmPassword').val('');
        $('#user-modal').modal();
    });

    $('#user-modal').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        if (button.data('action') !== undefined &&
            button.data('action') === 'editUser') {
            $('#userId').val(button.data('id'));
            $('#userName').val(button.data('uname'))
            $('#userName').prop('readonly', true);
            $('#userPassword').val('');
            $('#userConfirmPassword').val('');
        }
    });

    function newUserIsValid() {
        const userName = $('#userName').val();
        const password = $('#userPassword').val();
        const confirmPassword = $('#userConfirmPassword').val();

        if (userName === '' || password === '' || confirmPassword === '') {
            $('#user-modal-validation').html('All fields are required').show().delay(2000).fadeOut();
            return false;
        }

        if (password !== confirmPassword) {
            $('#user-modal-validation').html('Passwords not matching').show().delay(2000).fadeOut();
            return false;
        }

        return true;
    }

    $('#btnSaveUser').on('click', function () {
        if (newUserIsValid()) {
            var data = {
                'id': $('#userId').val(),
                'userName': $('#userName').val(),
                'password': $('#userPassword').val()
            };
            $.ajax({
                url: '/Admin/Settings?handler=User',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (!data.status) {
                    $('#user-modal-validation').html(data.message).show().delay(2000).fadeOut();
                } else {
                    $('#user-modal').modal('hide');
                    gridUsers.reload();
                    gridClientSiteAccess.reload();
                    showStatusNotification(true, 'Saved successfully');
                }
            }).fail(function () {
                console.log('error');
            }).always(function () {
            });
        }
    });

    gridClientSiteAccess = $('#user_client_access').grid({
        dataSource: '/Admin/Settings?handler=UserClientAccess',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'userName', title: 'User', width: 150 },
            { field: 'clientTypeCsv', title: 'Client Type Access', width: 250 },
            { field: 'clientSiteCsv', title: 'Client Site Access', width: 250 },
            { width: 100, tmpl: '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal" data-id="{id}"><i class="fa fa-pencil mr-2"></i>Edit</button>', align: 'center' },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    $('#user-client-access-modal').on('shown.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const userId = button.data('id');
        $(this).find('#user-access-for-id').val(userId);
        if (ucaTree === undefined) {
            ucaTree = $('#ucaTreeView').tree({
                uiLibrary: 'bootstrap4',
                checkboxes: true,
                primaryKey: 'id',
                dataSource: '/Admin/Settings?handler=ClientAccessByUserId',
                autoLoad: false,
                textField: 'name',
                childrenField: 'clientSites',
                checkedField: 'checked'
            });
        }
        ucaTree.uncheckAll();
        ucaTree.reload({ userId: userId });
    });

    $('#btnSaveUserAccess').on('click', function () {
        if (ucaTree) {
            const userId = $('#user-access-for-id').val();
            let selectedSites = ucaTree.getCheckedNodes().filter(function (item) {
                return item !== 'undefined';
            });
            $.ajax({
                url: '/Admin/Settings?handler=ClientAccessByUserId',
                data: {
                    userId: userId,
                    selectedSites: selectedSites
                },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridClientSiteAccess.reload();
                showStatusNotification(true, 'Saved successfully');
            }).fail(function () {
                console.log('error');
            });
        }
    });

    $('#grantAllUserAccess').on('click', function () {
        if (ucaTree !== undefined) {
            ucaTree.checkAll();
        }
    });

    $('#revokeAllUserAccess').on('click', function () {
        if (ucaTree !== undefined && confirm('Are you sure want to revoke all access?')) {
            ucaTree.uncheckAll();
        }
    });

    $('#expandAllUserAccess').on('click', function () {
        if (ucaTree !== undefined) {
            ucaTree.expandAll();
        }
    });

    $('#collapseAllUserAccess').on('click', function () {
        if (ucaTree !== undefined) {
            ucaTree.collapseAll();
        }
    });


    /****** Feedback Template Settings *******/
    $('#sel_fbktpl').on('change', function () {
        const selfeedback = $(this).val();
        if (selfeedback === '') {
            onNoneFeedbackTemplateSelected();
        } else {
            $('#form_fbktpl').show();
            $('#delete_fbktpl').show();
            $('#new_fbktpl_name').hide();
            $('#sel_fbktpl_name').html($("option:selected", this).text());
            $('#FeedbackTemplate_Id').val(selfeedback);
            $('#FeedbackTemplate_Name').val($("option:selected", this).text());
            $('#FeedbackTemplate_Type').prop('selectedIndex', 0);
            $.ajax({
                url: '/Admin/Settings?handler=FeedbackTemplate',
                type: 'GET',
                dataType: 'json',
                data: { templateId: selfeedback }
            }).done(function (data) {
                $('#FeedbackTemplate_Text').val(data.text);
                $('#FeedbackTemplate_Type').val(data.type);
            }).fail(function () {
                showStatusNotification(false, 'Something went wrong');
            });
        }
    });

    $('#add_fbktpl').on('click', function () {
        $('#sel_fbktpl').prop('selectedIndex', 0);
        $('#FeedbackTemplate_Id').val('')
        $('#FeedbackTemplate_Name').val('');
        $('#FeedbackTemplate_Text').val('');
        $('#FeedbackTemplate_Type').prop('selectedIndex', 0);
        $('#delete_fbktpl').hide();
        $('#sel_fbktpl_name').html('new template');
        $('#new_fbktpl_name').show();
        $('#form_fbktpl').show();
    });

    $('#save_fbktpl').on('click', function () {
        if ($('#FeedbackTemplate_Text').val() === '') {
            showModal('Pre-populated text for template');
            return false;
        }

        const isNewTemplate = $('#FeedbackTemplate_Id').val().length === 0;
        if (isNewTemplate) {
            let templateName = $('#FeedbackTemplate_Name').val().trim();
            if (templateName.length === 0) {
                showModal('Template name is required');
                return false;
            }

            let nameAlreadyExists = false;
            $('#sel_fbktpl > option').each(function () {
                nameAlreadyExists = this.text === templateName;
                if (nameAlreadyExists) return false;
            });
            if (nameAlreadyExists) {
                showModal('Template name ' + templateName + ' already exists!');
                return false;
            }
        }

        $.ajax({
            url: '/Admin/Settings?handler=FeedbackTemplate',
            type: 'POST',
            data: $('#form_fbktpl').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (data) {
            if (data.success && isNewTemplate) {
                refreshFeedbackTemplates();
                selectNoneFeedbackTemplate();
            }
            showStatusNotification(data.success, data.message);
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        });
    });

    $('#delete_fbktpl').on('click', function () {
        if (confirm('Are you sure want to delete this feedback template?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteFeedbackTemplate',
                type: 'POST',
                data: $('#form_fbktpl').serialize(),
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (data) {
                if (data.success) {
                    refreshFeedbackTemplates();
                    selectNoneFeedbackTemplate();
                }
                showStatusNotification(data.success, data.message);
            }).fail(function () {
                showStatusNotification(false, 'Something went wrong');
            });;
        }
    });

    const refreshFeedbackTemplates = function () {
        $.ajax({
            url: '/Admin/Settings?handler=FeedbackTemplateList',
            type: 'GET',
            success: function (data) {
                if (data) {
                    $('#sel_fbktpl').html('');
                    $('#sel_fbktpl').append('<option value="">None</option>');
                    data.map(function (template) {
                        $('#sel_fbktpl').append('<option value="' + template.id + '">' + template.name + '</option>');
                    });
                }
            }
        });
    }

    const selectNoneFeedbackTemplate = function () {
        $('#sel_fbktpl').prop('selectedIndex', 0);
        onNoneFeedbackTemplateSelected();
    }

    const onNoneFeedbackTemplateSelected = function () {
        $('#FeedbackTemplate_Id').val('');
        $('#FeedbackTemplate_Name').val('');
        $('#FeedbackTemplate_Text').val('');
        $('#FeedbackTemplate_Type').prop('selectedIndex', 0);
        $('#sel_fbktpl_name').html('');
        $('#new_fbktpl_name').hide();
        $('#delete_fbktpl').hide();
        $('#form_fbktpl').hide();
    }

    /****** IR Template Upload Settings *******/
    $('#ir_template_upload').on('change', function () {
        const file = $(this).get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || fileExtn !== 'pdf') {
            showModal('Unsupported file type. Please upload a .pdf file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);

        $.ajax({
            url: '/Admin/Settings?handler=IrTemplateUpload',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success)
                $('#ir_template_updatedOn').html(data.dateTimeUpdated);
            showStatusNotification(data.success, data.message);
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#ir_template_upload').val('');
        });
    });

    /****** Download Page Settings *******/

    $("#btn_DefaultEmailEdit").on("click", function () {
        var defaultMailEdit = $('#DefaultMailTextbox').text();
        $('#DefaultMailTextbox').toggle();
        $('#DefaultMail').hide();
        $('#btn_DefaultEmailUpdate').show();
        $('#btn_DefaultEmailEdit').hide();


    });
    $("#btn_DefaultEmailUpdate").on("click", function () {
        var defaultMailEdit = $('#DefaultMailTextboxval').val();
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(defaultMailEdit)) {

            showStatusNotification(false, 'Please enter a valid email address.');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=DefaultEmailUpdate',
            type: 'POST',
            data: { defaultMailEdit: defaultMailEdit }, // Send data as key-value pair
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (data) {
            if (data.success) {
                showStatusNotification(data.success, data.message);
            } else {
                showStatusNotification(false, 'Something went wrong');
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        });
    });
    let gridStaffDocs;
    let gridStaffDocsTypeCompanySop;
    let gridStaffDocsTypeTraining;
    let gridStaffDocsTypeTemplatesAndForms;



    gridStaffDocs = $('#staff_document_files').grid({
        dataSource: '/Admin/Settings?handler=StaffDocs',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 200 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 200 },
            { width: 200, renderer: staffDocsButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    /* Grid for CompanySop*/
    gridStaffDocsTypeCompanySop = $('#staff_document_files_type_CompanySop').grid({
        dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=1',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 200 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 200 },
            { width: 200, renderer: staffDocsButtonRendererCompanySop },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    /* Grid for Training*/
    gridStaffDocsTypeTraining = $('#staff_document_files_type_Training').grid({
        dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=2',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 200 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 200 },
            { width: 200, renderer: staffDocsButtonRendererTraining },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });


    gridStaffDocsTypeTemplatesAndForms = $('#staff_document_files_type_TemplatesAndForms').grid({
        dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=3',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 200 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 200 },
            { width: 200, renderer: staffDocsButtonRendererTemplatesAndForms },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    function staffDocsButtonRenderer(value, record) {
        return '<label class="btn btn-success mb-0">' +
            '<form id="form_file_downloads" method="post">' +
            '<i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>' +
            '<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>' +
            '<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file" data-doc-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>';
    }

    function staffDocsButtonRendererCompanySop(value, record) {
        return '<label class="btn btn-success mb-0">' +
            '<form id="form_file_downloads_company_sop" method="post">' +
            '<i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file_company_sop" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>' +
            '<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>' +
            '<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_company_sop" data-doc-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>';
    }
    function staffDocsButtonRendererTraining(value, record) {
        return '<label class="btn btn-success mb-0">' +
            '<form id="form_file_downloads_training" method="post">' +
            '<i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file_training" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>' +
            '<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>' +
            '<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_training" data-doc-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>';
    }
    function staffDocsButtonRendererTemplatesAndForms(value, record) {
        return '<label class="btn btn-success mb-0">' +
            '<form id="form_file_downloads_templates_forms" method="post">' +
            '<i class="fa fa-upload mr-2"></i>Replace' +
            '<input type="file" name="upload_staff_file_templates_forms" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
            '</form></label>' +
            '<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>' +
            '<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_templates_forms" data-doc-id="' + record.id + '"><i class="fa fa-trash mr-2"></i>Delete</button>';
    }

    $('#staff_document_files').on('click', '.delete_staff_file', function () {
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteStaffDoc',
                data: { id: $(this).attr('data-doc-id') },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridStaffDocs.reload();
            }).fail(function () {
                console.log('error')
            });
        }
    })

    $('#staff_document_files_type_CompanySop,#staff_document_files_type_Training,#staff_document_files_type_TemplatesAndForms').on('click', '.delete_staff_file_company_sop,.delete_staff_file_training,.delete_staff_file_templates_forms', function () {
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteStaffDoc',
                data: { id: $(this).attr('data-doc-id') },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridStaffDocsTypeCompanySop.reload();
                gridStaffDocsTypeTraining.reload();
                gridStaffDocsTypeTemplatesAndForms.reload();
            }).fail(function () {
                console.log('error')
            });
        }
    })

    $('#staff_document_files').on('change', 'input[name="upload_staff_file"]', function () {
        uploadStafDoc($(this), true);
    });

    $('#staff_document_files_type_CompanySop').on('change', 'input[name="upload_staff_file_company_sop"]', function () {
        uploadStafDocUsingType($(this), true, 1);
    });

    $('#staff_document_files_type_Training').on('change', 'input[name="upload_staff_file_training"]', function () {
        uploadStafDocUsingType($(this), true, 2);
    });

    $('#staff_document_files_type_TemplatesAndForms').on('change', 'input[name="upload_staff_file_templates_forms"]', function () {
        uploadStafDocUsingType($(this), true, 3);
    });

    $('#add_staff_document_file').on('change', function () {
        uploadStafDoc($(this));
    });
    $('#add_staff_document_file_company_sop').on('change', function () {
        uploadStafDocUsingType($(this), false, 1);
    });
    $('#add_staff_document_file_training').on('change', function () {
        uploadStafDocUsingType($(this), false, 2);
    });
    $('#add_staff_document_file_templates_and_forms').on('change', function () {
        uploadStafDocUsingType($(this), false, 3);
    });

    function uploadStafDoc(uploadCtrl, edit = false) {
        const file = uploadCtrl.get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || '.pdf,.docx,.xlsx'.indexOf(fileExtn.toLowerCase()) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx or .xlsx file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);
        if (edit)
            fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDoc',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success)
                gridStaffDocs.reload();
            showStatusNotification(data.success, data.message);
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        });
    }

    function uploadStafDocUsingType(uploadCtrl, edit = false, type) {
        var Email = $('#file_downloads').val();
        const file = uploadCtrl.get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || '.pdf,.docx,.xlsx'.indexOf(fileExtn.toLowerCase()) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx or .xlsx file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('type', type);
        if (edit)
            fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDocUsingType',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {
                gridStaffDocsTypeCompanySop.reload();
                gridStaffDocsTypeTraining.reload();
                gridStaffDocsTypeTemplatesAndForms.reload();
                showStatusNotification(data.success, data.message);
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        });
    }

    /****** Downloads *******/

    var queryString = window.location.search;
    // Parse the query string into an object
    var params = new URLSearchParams(queryString);
    // Get the value of the 'name' parameter
    var type = params.get('type');
    let gridFileDownloads;
    gridFileDownloads = $('#file_downloads').grid({
        /* dataSource: '/Admin/Settings?handler=StaffDocs',*/
        dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=' + type,
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'Files', width: 400 },
            { width: 100, renderer: fileDownloadsButtonRenderer },
        ]
    });

    function fileDownloadsButtonRenderer(value, record) {
        return '<div class="button-container-div"><a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a></div>'
    }

    const showStatusNotification = function (success, message) {
        if (success) {
            $('.toast .toast-header strong').removeClass('text-danger').addClass('text-success').html('Success');
        } else {
            $('.toast .toast-header strong').removeClass('text-success').addClass('text-danger').html('Error');
        }
        $('.toast .toast-body').html(message);
        $('.toast').toast('show');
    }

    const showModal = function (message) {

        $('#msg-modal .modal-body p').html(message);
        $('#msg-modal').modal();
    }
    /*****C4i Core Settings *****/

    getC4Settings();


    function getC4Settings() {

        const option = 1;

        $.ajax({
            url: '/Admin/Settings?handler=CoreSettings&companyId=' + option,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {

            for (var i = 0; i < data.length; i++) {
                $('#txt_CompanyId').val(data[i].id);
                $('#txt_CompanyName').val(data[i].name);
                $("#txt_CompanyDomain").val(data[i].domain);
                $("#txt_HomePageMessage").val(data[i].homePageMessage);
                $("#txt_HomePageMessage2").val(data[i].homePageMessage2);
                $("#txt_color").val(data[i].messageBarColour);
                $("#txt_BannerMessage").val(data[i].bannerMessage);
                $("#txt_HyplerLink").val(data[i].hyperlink);
                $("#txt_EmailMessage").val(data[i].emailMessage);
                $("#img_PrimaryLogo").attr('src', data[i].primaryLogoPath);
                $("#img_BannerLogo").attr('src', data[i].bannerLogoPath);
            }


            data.map(function (clientType) {
                $('#sel_client_type').append('<option value="' + clientType.id + '">' + clientType.name + '</option>');
            });

        });
    }


    $('#cr_primarylogo_upload').on('change', function () {

        const file = $(this).get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        // if (!fileExtn || fileExtn !== 'jpg' || fileExtn !=='JPG' || fileExtn !=='jpeg' || fileExtn !=='JPEG') {
        if (!fileExtn || (fileExtn !== 'jpg' && fileExtn !== 'JPG' && fileExtn !== 'jpeg' && fileExtn !== 'JPEG' && fileExtn !== 'png' && fileExtn !== 'PNG' && fileExtn !== 'GIF' && fileExtn !== 'gif')) {
            showModal('Unsupported file type. Please upload a .jpg/.jpeg file');
            return false;
        }
        const prlogopath = $("#img_PrimaryLogo").prop('src');
        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('prlogopath', prlogopath);
        $.ajax({
            url: '/Admin/Settings?handler=CrPrimaryLogoUpload',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {

            if (data.success)

                var result = prlogopath.lastIndexOf("/");
            const newfile = data.filepath;
            var url = window.location.origin;
            var result1 = newfile.lastIndexOf("/");
            var substr = prlogopath.substring(0, result - 1);

            var substr2 = newfile.substring(result1 + 1);
            substr = substr + "cr_primarylogo.JPG";
            var newpath = url + "/Images/cr_primarylogo.JPG";
            $("#img_PrimaryLogo").attr('src', newpath);

        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {

        });
    });



    $('#cr_bannerlogo_upload').on('change', function () {

        const file = $(this).get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        // if (!fileExtn || fileExtn !== 'jpg' || fileExtn !=='JPG' || fileExtn !=='jpeg' || fileExtn !=='JPEG') {
        if (!fileExtn || (fileExtn !== 'jpg' && fileExtn !== 'JPG' && fileExtn !== 'jpeg' && fileExtn !== 'JPEG' && fileExtn !== 'png' && fileExtn !== 'PNG' && fileExtn !== 'GIF' && fileExtn !== 'gif')) {
            showModal('Unsupported file type. Please upload a .jpg/.jpeg file');
            return false;
        }
        const prlogopath = $("#img_BannerLogo").prop('src');
        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('prlogopath', prlogopath);
        $.ajax({
            url: '/Admin/Settings?handler=CrBinaryLogoUpload',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {

            if (data.success)

                var result = prlogopath.lastIndexOf("/");
            var newfile = data.filepath;
            var result1 = newfile.lastIndexOf("/");
            var substr = prlogopath.substring(0, result + 1);

            var substr2 = newfile.substring(result1 + 1);
            substr = substr + "cr_bannerlogo.JPG";
            var url = window.location.origin;
            var newpath = url + "/Images/cr_bannerlogo.JPG";
            $("#img_BannerLogo").attr('src', newpath);



        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {

        });
    });




    $("#btn_CompanySave").on("click", function () {

        var companyId = parseInt($("#txt_CompanyId").val());
        if (ValidateCompany(true)) {
            const token = $('input[name="__RequestVerificationToken"]').val();
            var obj = {
                Id: companyId,
                Name: $("#txt_CompanyName").val(),
                Domain: $("#txt_CompanyDomain").val(),
                PrimaryLogoPath: $("#img_PrimaryLogo").prop('src'),
                BannerLogoPath: $("#img_BannerLogo").prop('src'),
                HomePageMessage: $("#txt_HomePageMessage").val(),
                HomePageMessage2: $("#txt_HomePageMessage2").val(),
                MessageBarColour: $("#txt_color").val(),
                BannerMessage: $("#txt_BannerMessage").val(),
                Hyperlink: $("#txt_HyplerLink").val(),
                EmailMessage: $("#txt_EmailMessage").val()
            }
            $.ajax({
                url: '/Admin/Settings?handler=CompanyDetails',
                data: { 'company': obj },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {
                if (result.status) {
                    if (result.message !== '') {
                        getC4Settings();
                        showStatusNotification(true, 'Company details modified successfully');

                    }

                } else {
                    displayGuardValidationSummary(result.message);
                }
            });
        }
    });
    function ValidateCompany() {

        if ($("#txt_CompanyName").val() == "") {
            showModal('Company name is required');
            return false;
        }
        return true;
    }




    /* Block Print Screen start 27092023 */
    function copyToClipboard() {
        /* when click Print screen it's copy a blank text in clipboard*/
        var textToCopy = '';
        navigator.clipboard.writeText(textToCopy);

    }

    $(window).keyup(function (e) {

        if (e.key === 'Alt' || e.key === 'PrintScreen' || e.key === 'Meta') {
            e.preventDefault();
            copyToClipboard();
        }
        if (e.key === 'Alt' && e.key === 'PrintScreen' || e.key === 'Meta') {
            e.preventDefault();
            copyToClipboard();
        }
        switch (e.keyCode) {
            case 49: // 1
                copyToClipboard();
                break;
            case 44: // PrintScreen
                copyToClipboard();
                break;
            case 91: // left windows
                copyToClipboard();
                break;
            case 92: // right windows
                copyToClipboard();
                break;
        }

    });

    document.addEventListener("keydown", onKeyDown, false);

    function onKeyDown(e) {

        if (e.key === 'Alt' || e.key === 'PrintScreen' || e.key === 'Meta') {
            e.preventDefault();
            copyToClipboard();
        }
        switch (e.keyCode) {
            case 49: // 1
                copyToClipboard();
                break;
            case 44: // PrintScreen
                copyToClipboard();
                break;
            case 91: // left windows
                copyToClipboard();
                break;
            case 92: // right windows
                copyToClipboard();
                break;
        }
    }
    /* Block Print Screen end */

    /* dark Mode Start*/
    $('#toggleDarkMode').on('change', function () {
        const isChecked = $(this).is(':checked');
        const filter = isChecked ? 1 : 2;
        // Check if dark mode is enabled
        var darkModeEnabled = $("body").hasClass("dark-mode");
        // Toggle the dark-mode class on the body element
        $("body").toggleClass("dark-mode", !darkModeEnabled);
        // Toggle dark mode for all other elements
        $("*").each(function () {
            $(this).toggleClass("dark-mode", !darkModeEnabled);
        });
        // Update the user's preference in local storage
        localStorage.setItem('darkMode', !darkModeEnabled);

    });
    /* On Each page load check if darmode set*/
    setDarkModePreference();
    function setDarkModePreference() {
        var check = localStorage.getItem('darkMode');
        var darkModeEnabled2 = localStorage.getItem('darkMode') === 'true';
        localStorage.setItem('darkMode', darkModeEnabled2);
        if (darkModeEnabled2) {
            $('#toggleDarkMode').prop('checked', true);
        }
        else {

            $('#toggleDarkMode').prop('checked', false);
        }

        if (darkModeEnabled2 != null) {
            $("body").toggleClass("dark-mode", darkModeEnabled2);
            $("*").each(function () {
                $(this).toggleClass("dark-mode", darkModeEnabled2);
            });
            $('table tbody tr').each(function () {
                $(this).toggleClass("dark-mode", darkModeEnabled2);
            });

        }
    }
    /* dark mode end */
    $('#register_plate_loaded').on('click', 'button[id=btn_delete_plate]', function () {


        var plateid = $(this).closest("tr").find("td").eq(1).text();

        var truckNo = $(this).closest("tr").find("td").eq(2).text();

        $(this).closest("tr").remove();
        var obj =
        {
            PlateId: plateid,
            TruckNo: truckNo,
            LogId: 0

        }

        $.ajax({
            url: '/Incident/Register?handler=DeletePlateLoaded',
            data: { 'report': obj },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.status) {
                if (result.message !== '') {

                    //getPlatesLoaded();
                    // showStatusNotification(true, 'Company details modified successfully');

                }

            } else {
                displayGuardValidationSummary(result.message);
            }
        });

        //$(this).closest("tr").remove();
        return false;
    });
});
/*for adding a reportlogo-start*/
$('#cr_reportlogo_upload').on('change', function () {

    const file = $(this).get(0).files.item(0);
    const fileExtn = file.name.split('.').pop();
    // if (!fileExtn || fileExtn !== 'jpg' || fileExtn !=='JPG' || fileExtn !=='jpeg' || fileExtn !=='JPEG') {
    if (!fileExtn || (fileExtn !== 'jpg' && fileExtn !== 'JPG' && fileExtn !== 'jpeg' && fileExtn !== 'JPEG' && fileExtn !== 'png' && fileExtn !== 'PNG' && fileExtn !== 'GIF' && fileExtn !== 'gif')) {
        showModal('Unsupported file type. Please upload a .jpg/.jpeg file');
        return false;
    }
    const prlogopath = $("#img_ReportLogo").prop('src');
    const fileForm = new FormData();
    fileForm.append('file', file);
    fileForm.append('prlogopath', prlogopath);
    $.ajax({
        url: '/Admin/Settings?handler=CrReportLogoUpload',
        type: 'POST',
        data: fileForm,
        processData: false,
        contentType: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    }).done(function (data) {

        if (data.success)

            var result = prlogopath.lastIndexOf("/");
        const newfile = data.filepath;
        var url = window.location.origin;
        var result1 = newfile.lastIndexOf("/");
        var substr = prlogopath.substring(0, result - 1);

        var substr2 = newfile.substring(result1 + 1);
        substr = substr + "CWSLogoPdf.png";
        var newpath = url + "/Images/CWSLogoPdf.png";
        $("#img_ReportLogo").attr('src', newpath);

    }).fail(function () {
        showStatusNotification(false, 'Something went wrong');
    }).always(function () {

    });
});
/*for adding a reportlogo-end*/