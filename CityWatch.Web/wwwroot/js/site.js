﻿$(function () {

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
    $('#Report_DateLocation_ClientArea').attr('placeholder', 'Select');
    $('#Report_DateLocation_ClientSite').editableSelect({
        //filter: false,
        effects: 'slide'
    })
        .on('select.editable-select', function (e, li) {
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
                    const clientAreaControl = $('#Report_DateLocation_ClientArea');
                    //p1 - 202 site allocation - start
                    clientAreaControl.html('');
                    toggleClientGpsLink(false);
                    /*p1-226 gps ir map issue -start*/
                    const ahrefElem = getGpsAsHyperLink(data.clientSite.gps);
                    $('#liveGpsWrapper').html(ahrefElem);
                    $('#Report_DateLocation_ClientSiteLiveGps').val(data.clientSite.gps);
                    toggleClientGpsLink(true, data.clientSite.gps);

                    //p1 - 226 gps ir map issue - end
                    //const ulClients = $('#Report_DateLocation_ClientArea').siblings('ul.es-list');
                    //ulClients.html('');

                    const option = $('#Report_DateLocation_ClientSite').val();
                    if (option == '')
                        return false;

                    $.ajax({
                        url: '/Incident/Register?handler=ClientAreas&Id=' + encodeURIComponent(option),
                        type: 'GET',
                        dataType: 'json',
                        success: function (data) {

                            data.map(function (site) {
                                // ulClients.append('<li class="es-visible" value="' + site.text + '">' + site.text + '</li>');
                                clientAreaControl.append('<option value="' + site.text + '">' + site.text + '</option>');

                            });

                        }
                    });


                    //p1 - 202 site allocation - end
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
        //p1 - 202 site allocation - start
        //else {
        //    // $('#Report_DateLocation_ClientArea').val('');
        //    const clientAreaControl = $('#Report_DateLocation_ClientArea');
        //    clientAreaControl.html('');
        //    toggleClientGpsLink(false);

        //    //const ulClients = $('#Report_DateLocation_ClientArea').siblings('ul.es-list');
        //    //ulClients.html('');

        //    const option = $(this).val();
        //    if (option == '')
        //        return false;

        //    $.ajax({
        //        url: '/Incident/Register?handler=ClientAreas&Id=' + encodeURIComponent(option),
        //        type: 'GET',
        //        dataType: 'json',
        //        success: function (data) {

        //            data.map(function (site) {
        //                // ulClients.append('<li class="es-visible" value="' + site.text + '">' + site.text + '</li>');
        //                clientAreaControl.append('<option value="' + site.text + '">' + site.text + '</option>');

        //            });

        //        }
        //    });


        //    //p1 - 202 site allocation - end
        //}
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

        //For setting background and font colour in the selected dropdown (refer setAllControlsForIrFromPreviousIR() in the Register.cshtml.cs file also.)
        $(this).attr('style', $(this).find('option:selected').attr('style'));
        //$(this).attr('style', 'background-color:white;');

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
            { field: 'name', title: 'Client Type', width: 400, editor: true },
            { title: '3rd Party', width: 50, renderer: domainSettings, cssClass: 'text-center' },
            { field: 'clientSiteCount', width: 50, editor: false, cssClass: 'text-center' }
        ],

        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
            //$(e.target).find('thead tr th:nth-last-child(2)').html('Σ')
            $(e.target).find('thead tr th:nth-last-child(2)').html('<span>&Sigma;</span>').css('text-align', 'center');
        }
    });




    function domainSettings(value, record) {

        // Check if the row is a newly added row by ID or another condition
        if (record.id === -1) {
            // Skip rendering the checkbox for new rows
            return '';
        }

        if (record.isSubDomainEnabled) {

            return '<a href="#siteTypeDomainDeatils" id="btnDomianDetailsforUser"><input type="checkbox" checked></a><input type="hidden" id="typeId" value="' + record.id + '"> <input type="hidden" id="typeName" value="' + record.name + '">'

        }
        else {
            return '<a href="#siteTypeDomainDeatils" id="btnDomianDetailsforUser"><input type="checkbox"></a><input type="hidden" id="typeId" value="' + record.id + '"> <input type="hidden" id="typeName" value="' + record.name + '">'
        }

    }

    $('#client_type_settings tbody').on('click', '#btnDomianDetailsforUser', function () {
        ClearModelControls();
        $('#siteTypeDomainDeatils').modal('show');
        var typeId = $(this).closest("td").find('#typeId').val();
        var typeName = $(this).closest("td").find('#typeName').val();
        //var userName = $(this).closest("td").find('#userName').val();
        $('#siteTypeDomainDeatils').find('#userName1').text(typeName)
        $('#siteTypeDomainDeatils').find('#siteTypeId').val(typeId)
        fetchUDomainDeatils(typeId);
    });

    function ClearModelControls() {
        // $('#siteTypeDomainDeatils').find('#userName').text('');               // Clear text
        $('#siteTypeDomainDeatils').find('#siteTypeId').val(0);               // Reset value to 0
        $('#siteTypeDomainDeatils').find('#Domainfilename').val('');                // Clear value
        $('#siteTypeDomainDeatils').find('#checkDomainStatus').prop('checked', false);  // Uncheck checkbox
        $('#siteTypeDomainDeatils').find('#domainName').val('');              // Clear domain name
        $('#siteTypeDomainDeatils').find('#add_ContractorLogo').val(null);
        $('#siteTypeDomainDeatils').find('#domainId').val(0);
        $('#ImageDiv').html('');

    }

    function fetchUDomainDeatils(typeId) {

        $.ajax({
            url: '/Admin/Settings?handler=DomainDetails',
            method: 'GET',
            data: { typeId: typeId },
            success: function (data) {
                if (data.success) {
                    ClearModelControls();
                    $('#siteTypeDomainDeatils').find('#siteTypeId').val(typeId)
                    $('#siteTypeDomainDeatils').find('#domainName').val(data.result.domain);
                    $('#siteTypeDomainDeatils').find('#checkDomainStatus').prop('checked', data.result.enabled);
                    $('#siteTypeDomainDeatils').find('#Domainfilename').val(data.result.logo);
                    $('#siteTypeDomainDeatils').find('#domainId').val(data.result.id);

                    $('#ImageDiv').html('<a href="../SubdomainLogo/' + data.result.logo + '" target="_blank"><img src="../SubdomainLogo/' + data.result.logo + '" alt="logo" style="width:50px;height:50px;" /></a> <a href="../SubdomainLogo/' + data.result.logo + '" target="_blank">' + data.result.logo + '</a>');
                    //$('#domainImage').attr('src', ');

                }
            },
            error: function (xhr, status, error) {
                console.error('Error fetching data:', error);
            }
        });
    }


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


    $('#siteTypeDomainDeatils').on('hidden.bs.modal', function (e) {
        // Code to run when the modal is closed
        if (gridType) {
            gridType.reload();

        }


    });

    function reloadgridType() {
        gridType.reload();
    }

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
            { field: 'clientType', title: 'Client Type', width: 170, renderer: function (value, record) { return value.name; } },
            { field: 'name', title: 'Client Site', width: 150, editor: true },
            { field: 'emails', title: 'Emails', width: 200, editor: true },
            { field: 'address', title: 'Address', width: 160, editor: addressEditor },
            { field: 'state', title: 'State', width: 70, type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=ClientStates', valueField: 'name', textField: 'name' } },
            { field: 'gps', title: 'GPS', width: 100, editor: gpsEditor, renderer: gpsRenderer },
            { field: 'billing', title: 'Billing', width: 100, editor: true },
            { field: 'status', title: 'Status', width: 110, renderer: statusTypeRenderer, editor: statusTypeEditor },
            { field: 'statusDate', hidden: true, editor: true },
            /*p1-245 jump button-start*/
            {
                field: 'statutypeIdsDate', title: 'Client Type', renderer: renderSiteTelematicsview, width: 100
            }
            /*p1 - 245 jump button - end*/
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');

            /*p1-245 jump button-start*/

            $(e.target).find('thead tr th:nth-last-child(2)').html('<i class="fa fa-bar-chart" aria-hidden="true"></i>').css('text-align', 'center');
            /*p1-245 jump button-end*/
        }
    });
    if ($('#sel_client_type').val() != null && $('#sel_client_type').val() != '' && $('#sel_client_type').val() != undefined) {

        gridSite.clear();
        gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val() });
    }
    function renderSiteTelematicsview(value, record, $cell, $displayEl) {
        //let $editBtn = $('<button id="btnEditClientSiteTelematicslink" class="btn btn-outline-primary mr-2" data-cs-typeid="' + record.typeId + '" data-cs-siteid="' + record.id + '" ><i class="fa fa-pencil">Edit</i></button>'
        //        );
        var securityNumber = $('#siteGuardSecurityLicenseNo').val();
        var siteGuardId = $('#siteGuardId').val();
        var siteloggedInUserId = $('#siteloggedInUserId').val();
        var type = 'settings';
        if (siteloggedInUserId == '' || siteloggedInUserId == null) {
            siteloggedInUserId = '0';
        }
        if (siteGuardId == '' || siteGuardId == null) {
            siteGuardId = '0';
        }

        let $editBtn = $('<a href="https://kpi.cws-ir.com/Dashboard?ClientTypeId=' + record.typeId + '&&ClientSiteId=' + record.id + '&&Sl=' + securityNumber + '&&lud=' + siteloggedInUserId + '&&guid=' + siteGuardId + '&&type=' + type + '" class="nav-link py-0" target="_blank"><i class="fa fa-pencil"></i>EDIT</a>'
        );

        //$editBtn.on('click', function (e) {
        //    //   gridSite.edit($(this).data('id'));
        //    var securityNumber = $('#siteGuardSecurityLicenseNo').val();
        //    var siteGuardId = $('#siteGuardId').val();
        //    var siteloggedInUserId = $('#siteloggedInUserId').val();
        //    var type='settings'
        //            /* $('#txt_securityLicenseNoIR').val('');*/
        //    window.open('https://localhost:44378/Dashboard?ClientTypeId=' + record.typeId + "&&ClientSiteId=" + record.id + "&&Sl=" + securityNumber + "&&lud=" + siteloggedInUserId + "&&guid=" + siteGuardId + "&&type=" + type, '_blank');
        //    //window.location.href = 'https://localhost:44378/Dashboard?ClientTypeId=' + record.typeId + "&&ClientSiteId=" + record.id + "&&Sl=" + securityNumber + "&&lud=" + siteloggedInUserId + "&&guid=" + siteGuardId + "&&type=" + type;
        //    //window.location.href = 'https://kpi.cws-ir.com/Admin/Dashboard?ClientTypeId=' + record.typeId + "&&ClientSiteId=" + record.id + "&&Sl=" + securityNumber + "&&lud=" + siteloggedInUserId + "&&guid=" + siteGuardId 

        //});



        $displayEl.empty().append($editBtn)

    }

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
            }).done(function (result) {
                if (result.status == false) {
                    alert(result.message);
                }

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
        //27/11/2027 commneted for expireing sites shows expired based on the date here
        // so the status not same in every area.So Commneted
        //if (value === 1 && record.formattedStatusDate && new Date(record.formattedStatusDate) < new Date())
        //   value = 2;

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
            gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $(this).val(), searchTermtwo: $('#search_kw_client_site_email').val() });
        }
    });


    $('#btnSearchClientSite').on('click', function () {
        gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val(), searchTermtwo: $('#search_kw_client_site_email').val() });
    });
    /*p1-192 client site email seach-start*/
    $('#btnSearchClientEmail').on('click', function () {
        gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val(), searchTermtwo: $('#search_kw_client_site_email').val() });
    });
    $('#search_kw_client_site_email').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridSite.reload({ typeId: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val(), searchTermtwo: $(this).val() });
        }
    });
    /* p1 - 192 client site email seach - end*/

    //Duress App start
    let gridDuressAppLogFields;
    let isDuressAppFieldAdding = false;
    gridDuressAppLogFields = $('#tbl_duressapp_fields').grid({
        dataSource: '/Admin/Settings?handler=DuressAppDetails',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        /*selectionType: 'multiple',*/
        button: true,
        inlineEditing: { mode: 'command' },

        columns: [
            { field: 'label', title: 'Label', width: '100%', editor: true },
            { field: 'name', title: 'Text to Stamp into LB', width: '100%', editor: true },
            
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    let gridDuressAppAudioFields;
    let isDuressAppFieldAudioAdding = false;
    gridDuressAppAudioFields = $('#tbl_duressappLog_fields').grid({
        dataSource: '/Admin/Settings?handler=DuressAppDetails',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        /*selectionType: 'multiple',*/
        button: true,
        //inlineEditing: { mode: 'command' },

        columns: [
            { field: 'label', title: 'Label', width: '100%', editor: false },
            { field: 'name', title: 'File Name', width: '100%', editor: false },
            { width: 166, renderer: schButtonRendererAudio },


        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    function schButtonRendererAudio(value, record) {
        let buttonHtml = '';
        buttonHtml = '<a href="/DuressAppAudio/' + record.name + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-play"></i></a>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary m-1 d-block" data-toggle="modal" data-target="#DuressAppAudio-modal" data-au-id="' + record.id + '" ';
        buttonHtml += 'data-action="editAudio"><i class="fa fa-pencil"></i></button>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-danger m-1 del-duressAudio d-block" data-aud-id="' + record.id + '""><i class="fa fa-trash" aria-hidden="true"></i></button>';
        return buttonHtml;
    }


    let gridDuressAppMultimediaFields;
    let isDuressAppFieldMultimediaAdding = false;
    gridDuressAppMultimediaFields = $('#tbl_duressappMultimedia_fields').grid({
        dataSource: '/Admin/Settings?handler=DuressAppDetails',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        /*selectionType: 'multiple',*/
        button: true,
        //inlineEditing: { mode: 'command' },

        columns: [
            { field: 'label', title: 'Label', width: '100%', editor: false },
            { field: 'name', title: 'File Name', width: '100%', editor: false },
            { width: 166, renderer: schButtonRendererMultimedia },


        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    function schButtonRendererMultimedia(value, record) {
        let buttonHtml = '';
        buttonHtml = '<a href="/DuressAppMultimedia/' + record.name + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-play"></i></a>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary m-1 d-block" data-toggle="modal" data-target="#DuressAppMiltimedia-modal" data-au-id="' + record.id + '" ';
        buttonHtml += 'data-action="editMultimedia"><i class="fa fa-pencil"></i></button>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-danger m-1 del-duressMultimedia d-block" data-aud-id="' + record.id + '""><i class="fa fa-trash" aria-hidden="true"></i></button>';
        return buttonHtml;
    }
    $('#tbl_duressappMultimedia_fields').on('click', '.del-duressMultimedia', function () {
        const idToDelete = $(this).attr('data-aud-id');
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteDuressApp',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridDuressAppMultimediaFields.reload({ typeId: $('#duressapp_types').val() });
            });
        }

    });
    $('#tbl_duressappLog_fields').on('click', '.del-duressAudio', function () {
        const idToDelete = $(this).attr('data-aud-id');
        if (confirm('Are you sure want to delete this file?')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteDuressApp',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridDuressAppAudioFields.reload({ typeId: $('#duressapp_types').val() });
            });
        }

    });
    $('#DuressAppAudio-modal').on('shown.bs.modal', function (event) {
        clearAudioModal();

        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editAudio';
        if (isEdit) {
            schId = button.data('au-id');
            DuressAppModalOnEdit(schId);
        }
    });
    $('#DuressAppMiltimedia-modal').on('shown.bs.modal', function (event) {
        clearMultimediaModal();

        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editMultimedia';
        if (isEdit) {
            schId = button.data('au-id');
            DuressAppModalOnEditMultimedia(schId);
        }
    });
    function clearAudioModal() {

        $('#filenameaudio').val('');
        $('#add_label').val('');
        $('#add_filenameduress').val('');
        $('#dynamicAudio').html('');
        $('#audioId').val('-1');
    }
    function clearMultimediaModal() {

        $('#filenamemultimedia').val('');
        $('#add_label1').val('');
        $('#add_multimediaduress').val('');
        $('#dynamicMultimedia').html('');
        $('#multimediaId').val('-1');
    }
    function DuressAppModalOnEdit(AudioId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/GuardSettings?handler=AudioDetails&id=' + AudioId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
           
            $('#add_label').val(data.label);
            $('#audioId').val(data.id);
            $('#filenameaudio').val(data.name);
            $('#dynamicAudio').html('<a href="/DuressAppAudio/' + data.name + '" class="btn btn-outline-primary" target="_blank"><i class="fa fa-play"></i></a>');


        }).always(function () {
            $('#loadinDivAudio').hide();
        });
    }

    function DuressAppModalOnEditMultimedia(MultimediaId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/GuardSettings?handler=AudioDetails&id=' + MultimediaId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {

            $('#add_label1').val(data.label);
            $('#multimediaId').val(data.id);
            $('#filenamemultimedia').val(data.name);
            $('#dynamicMultimedia').html('<a href="/DuressAppMultimedia/' + data.name + '" class="btn btn-outline-primary" target="_blank"><i class="fa fa-play"></i></a>');


        }).always(function () {
            $('#loadinDivMultimedia').hide();
        });
    }

    $('#duressapp_types').on('change', function () {
        const selKvlFieldTypeId = $('#duressapp_types').val();
       
        if (selKvlFieldTypeId == 2) {
            $('#add_duressappMultimedia_fields').hide();
            $('#add_duressapp_fields').show();
            gridDuressAppAudioFields.hide();
            gridDuressAppLogFields.show();
            gridDuressAppLogFields.clear();
            gridDuressAppLogFields.reload({ typeId: selKvlFieldTypeId });
            $('#add_DuressAppAudio').hide();
            gridDuressAppMultimediaFields.hide();

        }
        else if (selKvlFieldTypeId == 1) {
            $('#add_duressappMultimedia_fields').hide();
            $('#add_duressapp_fields').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.show();
            gridDuressAppAudioFields.clear();
            gridDuressAppAudioFields.reload({ typeId: selKvlFieldTypeId });
            $('#add_DuressAppAudio').show();
            gridDuressAppMultimediaFields.hide();


        }
        else if (selKvlFieldTypeId == 3) {
            $('#add_duressappMultimedia_fields').show();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppLogFields.hide();
            gridDuressAppMultimediaFields.show();
            gridDuressAppMultimediaFields.clear();
            gridDuressAppMultimediaFields.reload({ typeId: selKvlFieldTypeId });
        }
        else {
            $('#add_duressapp_fields').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            $('#add_duressappMultimedia_fields').hide();
            $('#add_DuressAppAudio').hide();
            gridDuressAppMultimediaFields.hide();
        }

        
    });
    let isDuressFieldAdding = false;
    $('#add_duressapp_fields').on('click', function () {
        const selFieldTypeId = $('#duressapp_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        if (selFieldTypeId == 1) {
            if (isDuressAppFieldAudioAdding) {
                alert('Unsaved changes in the grid. Refresh the page');
            } else {
                isDuressFieldAdding = true;
                gridDuressAppAudioFields.addRow({
                    'id': -1,
                    'typeId': selFieldTypeId,
                    'name': '',
                }).edit(-1);
            }
        }
        else {
            if (selFieldTypeId == 2) {
                if (isDuressAppFieldAdding) {
                    alert('Unsaved changes in the grid. Refresh the page');
                } else {
                    isDuressFieldAdding = true;
                    gridDuressAppLogFields.addRow({
                        'id': -1,
                        'typeId': selFieldTypeId,
                        'name': '',
                    }).edit(-1);
                }
            }
        }
       
    });
    if (gridDuressAppLogFields) {
        gridDuressAppLogFields.on('rowDataChanged', function (e, id, record1) {
            const data1 = $.extend(true, {}, record1);
            console.log(data1);
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveDuressApp',
                data: { record: data1 },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) {

                    gridDuressAppLogFields.reload({ typeId: $('#duressapp_types').val() });
                }
                else {
                    alert(result.message);
                   
                    gridDuressAppLogFields.edit(id);


                }

            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isDuressAppFieldAdding)
                    isDuressAppFieldAdding = false;
            });
        });

        gridDuressAppLogFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteDuressApp',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridDuressAppLogFields.reload({ typeId: $('#duressapp_types').val() });
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isDuressAppFieldAdding)
                        isDuressAppFieldAdding = false;
                });
            }
        });

    }

   

    $('#btnSaveAudioFile').on('click', function () {
        $('#loadinDivAudio').show();
        var fileName = $('#filenameaudio').val();
        var label = $('#add_label').val();
        
        var TypeId = $('#duressapp_types').val();
        
        var fileInput = $('#add_filenameduress')[0]; 
        var Id = $('#audioId').val();
        const fileForm = new FormData();
       

        if (fileInput.files.length > 0) {
            const file = fileInput.files[0]; 
            const fileExtn = file.name.split('.').pop().toLowerCase(); 
            fileName = file.name;

            fileForm.append('file', file);

        }
        else {

            fileForm.append('file', '');
        }
        if (fileName == '') {
            showModal('Please select the file to upload');
            return false;
        }
        fileForm.append('typeId', TypeId);
        fileForm.append('label', label);
        fileForm.append('name', fileName);
        fileForm.append('id', Id);
        
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveAudio',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {
                $('#DuressAppAudio-modal').modal('hide');

                gridDuressAppAudioFields.reload({ typeId: $('#duressapp_types').val() });
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#loadinDivAudio').hide();
        });



    });
    $("#add_multimediaduress").on("change", function () {
        var maxSize = 5 * 1024 * 1024;
        if (file.size > maxSize) {
            alert('File size exceeds 5MB');
            $('#loadinDivMultimedia').hide();
            return false; // Stop execution
        }
    });
    $('#btnSaveMultimedia').on('click', function () {
        $('#loadinDivMultimedia').show();
        var fileName = $('#filenamemultimedia').val();
        var label = $('#add_label1').val();

        var TypeId = $('#duressapp_types').val();

        var fileInput = $('#add_multimediaduress')[0];
        var Id = $('#multimediaId').val();
        const fileForm = new FormData();


        if (fileInput.files.length > 0) {
            const file = fileInput.files[0];
            const fileExtn = file.name.split('.').pop().toLowerCase();
            fileName = file.name;
           
            fileForm.append('file', file);

        }
        else {

            fileForm.append('file', '');
        }
        if (fileName == '') {
            showModal('Please select the file to upload');
            return false;
        }
        
        fileForm.append('typeId', TypeId);
        fileForm.append('label', label);
        fileForm.append('name', fileName);
        fileForm.append('id', Id);

        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveMultimedia',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {
                $('#DuressAppMiltimedia-modal').modal('hide');

                gridDuressAppMultimediaFields.reload({ typeId: $('#duressapp_types').val() });
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#loadinDivMultimedia').hide();
        });



    });

     //Duress App stop


    /****** Report Fileds start *******/
    let gridReportFields;

    gridReportFields = $('#field_settings').grid({
        dataSource: '/Admin/Settings?handler=ReportFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        /*selectionType: 'multiple',*/
        button: true,
        inlineEditing: { mode: 'command' },

        columns: [
            { field: 'name', title: 'Name', width: '100%', editor: true },
            { field: 'emailTo', title: 'Special Email Condition', width: '100%', editor: true },
            { field: 'stampRcLogbook', title: 'Stamp RC Logbook ?', type: 'checkbox', align: 'center', width: '70%', editor: true }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    let gridAreaReportFields;

    gridAreaReportFields = $('#field_settings_Area').grid({
        dataSource: '/Admin/Settings?handler=AreaReportFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        selectionType: 'multiple',
        button: true,
        //inlineEditing: { mode: 'command' },

        columns: [
            { field: 'name', title: 'Name', width: '100%' },
            { field: 'emailTo', title: 'Special Email Condition', width: '100%' },
            // { field: 'emailTo', title: 'State', width: 80, type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=ClientStates', valueField: 'name', textField: 'name' } },
            { field: 'clientSiteIds', hidden: true },
            { field: 'clientTypeIds', hidden: true },
            { field: 'clientTypes', title: 'Client Types', hidden: true },
            /*{ field: 'clientSites', title: 'Site Allocation', type: 'dropdown', width: '100%', type: 'button', editor: select2editor }*/
            { field: 'clientSites', title: 'Site Allocation', width: '100%' },
            {
                width: '100%', renderer: irButtonRenderer
            }

        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    /*p1-202 site allocation-start*/
    function irButtonRenderer(value, record) {
        return '<button id="btnEditIrGroup" data-irfield-id="' + record.id + '" data-irfield_typeid="' + record.typeId + '" data-irfield-name="' + record.name + '" data-ir-emailto="' + record.emailTo + '" data-ir-clientsiteids="' + record.clientSiteIds + '"data-ir-clientsites="' + record.clientSites + '"data-ir-clienttypeids="' + record.clientTypeIds + '"data-ir-clienttypes="' + record.clientTypes + '" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil mr-2"></i>Edit</button>' +
            '<button id="btnDeleteIrGroup" data-irfield-id="' + record.id + '"  class="btn btn-outline-danger"><i class="fa fa-trash mr-2"></i>Delete</button>' +

            '</div>'
    }
    $('#field_settings_Area tbody').on('click', '#btnEditIrGroup', function () {

        ClearIrSettings();
        $('#IrSettings_Id').val($(this).attr('data-irfield-id'));
        $('#Irfieldtype_Id').val($(this).attr('data-irfield_typeid'));
        $('#IrSettings_fieldName').val($(this).attr('data-irfield-name'));
        $('#IrSettings_fieldemailto').val($(this).attr('data-ir-emailto'));
        var name = $(this).attr('data-ir-clientsites');
        $('.multiselect [title="' + $(this).attr('data-ir-clientsites') + '"');
        // $('.multiselect').text($(this).attr('data-ir-clientsites'));
        clientSitesforReportsnew = $(this).attr('data-ir-clientsiteids');
        var selectedValues = $(this).attr('data-ir-clientsiteids').split(';');
        var newselectedvalues = [];
        var selectedValuesClientType = $(this).attr('data-ir-clienttypeids').split(';');
        var newselectedvaluesClientType = [];
        selectedValuesClientType.forEach(function (value) {
            var newvalue = parseInt(value);
            var stringnewvalue = String(newvalue)
            newselectedvaluesClientType.push(newvalue);
            $('#list_IrClientTypes option[value="' + value + '"]').attr('selected', true);
        });
        newselectedvaluesClientType.forEach(function (value) {

            $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
            $(".multiselect-option input[type=checkbox][value='" + value + "']").parent().parent().addClass('active');

        });
        $('#list_IrClientTypes').val(newselectedvaluesClientType);
        //clientSiteChange();
        const clientType = $('#list_IrClientTypes').val().join(';');
        const clientSiteControl = $('#list_IrClientSites');
        // keyVehicleLogReport.clear().draw();

        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesWithTypeId&types=' + clientType,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
                selectedValues.forEach(function (value) {
                    var newvalue = parseInt(value);
                    var stringnewvalue = String(newvalue)
                    newselectedvalues.push(newvalue);
                    $('#list_IrClientSites option[value="' + value + '"]').attr('selected', true);
                });
                newselectedvalues.forEach(function (value) {

                    $(".multiselect-option input[type=checkbox][value='" + value + "']").prop("checked", true);
                    $(".multiselect-option input[type=checkbox][value='" + value + "']").parent().parent().addClass('active');

                });
            }
        });
        //selectedValues.forEach(function (value) {
        //    var newvalue = parseInt(value);
        //    var stringnewvalue = String(newvalue)
        //    newselectedvalues.push(newvalue);
        //    $('#list_IrClientSites option[value="' + value + '"]').attr('selected', true);
        //});



        //$('#list_hrGroups').val($(this).attr('data-doc-hrgroupid'));
        //$('#list_ReferenceNoNumber').val($(this).attr('data-doc-refnonumberid'));
        //$('#list_ReferenceNoAlphabet').val($(this).attr('data-doc-refalphnumberid'));
        //$('#txtHrSettingsDescription').val($(this).attr('data-doc-description'));
        $('#irSettingsModal').modal('show');
        //selectedValues.forEach(function (valuenew) {

        //    //select.valueField = valuenew;
        //    var newvalue = parseInt(valuenew);
        //    //select.val(valuenew);
        //    $('#list_IrClientSites option[value="' + newvalue + '"]').prop('selected', true);

        //});



        $('#list_IrClientSites').val(newselectedvalues);

        //("#list_IrClientSites").multiselect('refresh');

    });
    function clientSiteChange() {
        const clientType = $('#list_IrClientTypes').val().join(';');
        const clientSiteControl = $('#list_IrClientSites');
        // keyVehicleLogReport.clear().draw();

        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesWithTypeId&types=' + clientType,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
            }
        });

        clientTypesforReportsnew = clientType;
    }
    $('#field_settings_Area tbody').on('click', '#btnDeleteIrGroup', function () {
        // var data = keyVehicleLog.row($(this).parents('tr')).data();
        if (confirm('Are you sure want to delete this  entry?')) {
            $.ajax({
                type: 'POST',
                url: '/Admin/Settings?handler=DeleteReportField',
                data: { 'id': $(this).attr('data-irfield-id') },
                dataType: 'json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                beforeSend: function () {
                    $('#loader').show();
                }
            }).done(function () {
                gridAreaReportFields.reload();
            }).always(function () {
                $('#loader').hide();
            });
        }
    });
    function select2editor($editorContainer, value, record) {
        // var select = $('<button class="btn btn-primary" id="generate_kvl_docket">Generate Docket</button>');
        var select = $('<select class="form-control mx-1 " multiple="multiple" id="vklClientSiteIdforir"></select>');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSites',
            // data: { id: record },
            //type: 'POST',
            //headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            for (var i = 0; i < result.length; i++) {
                //select.valueField = result[i].name;
                //select.textField = result[i].name;
                var newoption = '<option value= " ' + result[i].id + ' " > ' + result[i].name + '</option >';
                select.append(newoption);
            }
            var selectedValues = record.clientSiteIds.split(';');
            selectedValues.forEach(function (valuenew) {

                //select.valueField = valuenew;
                //select.val(valuenew);
                $('#vklClientSiteIdforir option[value="' + valuenew + '"]').prop('selected', true);

            });
            //$.each(item1 in result)
            //{
            //    '< option value = "' + item.name + '" >' + item.name +'</option >'
            //}
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isReportFieldAdding)
                isReportFieldAdding = false;
        })
        $editorContainer.append(select);
        //select.multiselect({
        //    maxHeight: 400,
        //    buttonWidth: '100%',
        //    nonSelectedText: 'Select',
        //    buttonTextAlignment: 'left',
        //    includeSelectAllOption: true,
        //});
        select.select2();


    }
    $('#list_IrClientSites').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });
    $('#list_IrClientTypes').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'Select',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    var clientSitesforReportsnew;
    var clientTypesforReportsnew;
    $('#field_settings tbody').on('change', '#vklClientSiteIdforir', function () {

        const clientSitesforReports = $(this).val().join(';');
        clientSitesforReportsnew = clientSitesforReports;
    });

    $('#list_IrClientSites').on('change', function () {
        const clientSitesforReports = $(this).val().join(';');
        clientSitesforReportsnew = clientSitesforReports;
        //var selectedValues = $(this).val();
        //$('#ClientSitePocIdsVehicleLog').val(selectedValues);


    });
    $('#btn_save_ir_settings').on('click', function () {
        var form = document.getElementById('form_new_hr_settings');
        var jsformData = new FormData(form);
        var id = $('#IrSettings_Id').val();
        if (id == '') {
            $('#IrSettings_Id').val(-1);
        }
        var data = {
            'Id': $('#IrSettings_Id').val(),
            'TypeId': $('#report_field_types').val(),
            'Name': $('#IrSettings_fieldName').val(),
            'EmailTo': $('#IrSettings_fieldemailto').val(),
            'ClientSiteIds': clientSitesforReportsnew,
            'ClientTypeIds': clientTypesforReportsnew
        };
        if ($('#IrSettings_fieldName').val() == '') {
            alert('Please Enter IR Field Name')
        }

        else {

            $.ajax({
                url: '/Admin/Settings?handler=ReportField',
                data: { reportfield: data },
                type: 'POST',
                //data: jsformData,

                //processData: false,
                //contentType: false,
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                beforeSend: function () {
                    $('#loader').show();
                }
            }).done(function (result) {
                if (result.status) {
                    isReportFieldAdding = false;

                    $('#irSettingsModal').modal('hide');
                    gridAreaReportFields.clear();
                    gridAreaReportFields.reload();
                    $('#IrSettings_Id').val('');
                    $('#Irfieldtype_Id').val('');
                    $('#IrSettings_fieldName').val('');
                    $('#IrSettings_fieldemailto').val('');
                }
                //else {
                //    displayValidationSummaryIrSettings(result.errors);
                //}
            }).always(function () {
                $('#loader').hide();
            });

        }

    });
    function displayValidationSummaryIrSettings(errors) {
        const summaryDiv = document.getElementById('irsettings-field-validation');
        summaryDiv.className = "validation-summary-errors";
        summaryDiv.querySelector('ul').innerHTML = '';
        errors.forEach(function (item) {
            const li = document.createElement('li');
            li.appendChild(document.createTextNode(item));
            summaryDiv.querySelector('ul').appendChild(li);
        });
    }

    $('#list_IrClientTypes').on('change', function () {

        const clientType = $(this).val().join(';');
        const clientSiteControl = $('#list_IrClientSites');
        // keyVehicleLogReport.clear().draw();

        clientSiteControl.html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesWithTypeId&types=' + clientType,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
            }
        });

        clientTypesforReportsnew = clientType;
    });
    /*p1 - 202 site allocation - end*/
    let isReportFieldAdding = false;

    $('#add_field_settings').on('click', function () {
        const selFieldTypeId = $('#report_field_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        if (selFieldTypeId != 4) {
            if (isReportFieldAdding) {
                alert('Unsaved changes in the grid. Refresh the page');
            }
            else {
                gridReportFields.addRow({
                    'id': -1,
                    'typeId': selFieldTypeId,
                    'name': '',
                    'emailTo': '',
                    'stampRcLogbook': false
                }).edit(-1);
            }
        } else {
            isReportFieldAdding = true;

            $('#irSettingsModal').modal('show');
            ClearIrSettings();
            $('#Irfieldtype_Id').val($('#report_field_types').val());
            var type = $('#report_field_types').find("option:selected").text();
            $('#lblFieldType').html(type);
        }
    });
    function ClearIrSettings() {
        $('#IrSettings_fieldName').val('');
        $(".multiselect-option input[type=checkbox]").prop("checked", false);
        $(".multiselect-option input[type=checkbox]").parent().parent().removeClass('active');
        $('#list_IrClientSites').val('');
        $('#IrSettings_fieldemailto').val('');

    }
    $('#report_field_types').on('change', function () {
        const selFieldTypeId = $(this).val();

        if (!selFieldTypeId) { // None
            $('#fieldSettings').show();
            $('#positionSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#irNotes').hide();

            gridReportFields.clear();
            gridPositions.clear();
            gridAreaReportFields.clear();
            gridReportFields.reload({ typeId: selFieldTypeId });
            gridAreaReportFields.hide();

        } else if (selFieldTypeId === '1') { // Position
            $('#fieldSettings').hide();
            $('#positionSettings').show();
            $('#PSPFSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#irNotes').hide();

            gridReportFields.clear();
            gridPositions.reload();
            gridAreaReportFields.clear();
            gridPSPF.clear();
            gridAreaReportFields.hide();
        }
        else if (selFieldTypeId === '5') {
            $('#PSPFSettings').show();
            $('#fieldSettings').hide();
            $('#positionSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#irNotes').hide();
            gridPositions.clear();
            gridReportFields.clear();
            gridAreaReportFields.clear();
            gridPSPF.reload();
            gridAreaReportFields.hide();
        }
        else if (selFieldTypeId === '6') {

            getIrEmailCCforReimbursement();
            $('#FinancialReimbursementSettings').show();
            $('#PSPFSettings').hide();
            $('#fieldSettings').hide();
            $('#positionSettings').hide();
            $('#irNotes').hide();

            gridPositions.clear();
            gridReportFields.clear();
            gridAreaReportFields.clear();
            gridPSPF.clear();
            gridAreaReportFields.hide();
        }
        else if (selFieldTypeId === '4') {
            $('#irNotes').hide();
            $('#fieldSettings').show();
            $('#positionSettings').hide();
            $('#PSPFSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#field_settings').hide();
            $('#field_settings_Area').attr('hidden', false);
            gridPSPF.clear();
            gridPositions.clear();
            gridReportFields.clear();
            gridAreaReportFields.show();
            gridAreaReportFields.reload({ typeId: selFieldTypeId });


        }
        else if (selFieldTypeId === '7') {

            $('#irNotes').show();
            $('#FinancialReimbursementSettings').hide();
            $('#PSPFSettings').hide();
            $('#fieldSettings').hide();
            $('#positionSettings').hide();


            gridPositions.clear();
            gridReportFields.clear();
            gridAreaReportFields.clear();
            gridPSPF.clear();
            gridAreaReportFields.hide();
        }
        else {
            $('#fieldSettings').show();
            $('#positionSettings').hide();
            $('#PSPFSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#field_settings').show();
            $('#field_settings_Area').hide();
            $('#irNotes').hide();

            gridPSPF.clear();
            gridPositions.clear();
            gridAreaReportFields.clear();
            gridAreaReportFields.hide();
            gridReportFields.reload({ typeId: selFieldTypeId });

        }
    });

    if (gridReportFields) {
        gridReportFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            data.clientSiteIds = clientSitesforReportsnew;
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

    var editPositionGridRender;
    editPositionGridRender = function (value, record, $cell, $displayEl, id, $grid) {
        var data = $grid.data(),
            $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
            $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_training" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
            $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
            $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
        $edit.on('click', function (e) {
            $grid.edit($(this).data('key'));
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        });
        $delete.on('click', function (e) {
            $grid.removeRow($(this).data('key'));
        });
        $update.on('click', function (e) {
            $grid.update($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $cancel.on('click', function (e) {
            $grid.cancel($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $displayEl.empty().append($edit).append($delete).append($update).append($cancel);
    }

    let gridPositions;

    gridPositions = $('#position_settings').grid({
        dataSource: '/Admin/Settings?handler=Positions',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [
            { field: 'name', title: 'Name', width: 230, editor: true },
            { field: 'emailTo', title: 'Special Email Condition', width: 200, editor: true },
            { field: 'isPatrolCar', title: 'Patrol Car?', type: 'checkbox', align: 'center', width: 80, editor: true },
            { field: 'dropboxDir', title: 'Dropbox Directory', width: 250, editor: true },
            {
                field: 'isLogbook', title: 'Logbook',
                type: 'checkbox', align: 'center', width: 100, editor: true,
            },
            { field: 'clientsiteName', title: 'Nominated logbook', width: 130, editor: false },

            {
                field: 'isSmartwandbypass', title: 'Smart WAND Bypass',
                type: 'checkbox', align: 'center', width: 100, editor: true,
                editor: {
                    // Assign a unique ID if needed
                    class: 'is-smartwandbypass-checkbox',
                    id: 'smartwandbypass' // Example of dynamic ID assignment
                }
            },
            { title: '<i class="fa fa-cogs" aria-hidden="true"></i>', width: 30, align: 'center', renderer: editPositionGridRender }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>').css('width', '116px');
        }
    });







    //To Position Checkbox and Popup start
    $('#PositionClientSiteId').select({
        placeholder: 'Select',
        theme: 'bootstrap4'
    });
    $('#position_settings').on('click', 'input[type="checkbox"]', function () {
        var itemId = $(this).data('id');
        var checkboxId = $(this).attr('id');
        var isChecked = $(this).is(':checked');
        var isSmartwandbypass = $(this).hasClass('is-smartwandbypass-checkbox');

        if (itemId === 'smartwandbypass') {
            // Skip modal popup for 'IsSmartwandbypass'
            return;
        }
        if (isChecked == true) {
            $('#po_client_type').val('');
            $('#PositionClientSiteId').val('');
            $('#PositionModel').modal('show');
        }

    });
    $('#po_client_type').on('change', function () {
        const clientTypeId = $(this).val();
        const clientSiteControl = $('#PositionClientSiteId');
        clientSiteControl.html('');


        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesNew',
            type: 'GET',
            data: {
                typeId: clientTypeId

            },
            dataType: 'json',
            success: function (data) {
                $('#PositionClientSiteId').append(new Option('Select', '', true, true));
                data.map(function (site) {
                    $('#PositionClientSiteId').append(new Option(site.name, site.id, false, false));
                });

            }


        });
    });
    //To Position Checkbox and Popup stop
    if (gridPositions) {
        gridPositions.on('rowDataChanged', function (e, id, record) {
            var Clientsiteid = '';
            if (record.isLogbook == false) {
                Clientsiteid = 0;
            }
            else {
                Clientsiteid = $('#PositionClientSiteId').val();
            }

            record.clientsiteId = Clientsiteid;
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
        if (confirm('Are you sure want to Activate this user?')) {
            toggleUserStatus($(this).attr('data-user-id'), false);
        }
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
            { field: 'userName', title: 'User Name', width: 100 },
            { title: 'Password', width: 100, renderer: passwordRenderer },
            { title: 'Activity', width: 350, renderer: activityDeatils },
            { field: 'isDeleted', title: 'Deleted?', align: 'center', width: 75, renderer: function (value) { return value ? 'Yes' : '&nbsp;'; } },
            { width: 100, renderer: userButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });


    /*p1-248 search users grid*/

    function activityDeatils(value, record) {
        if (record.formattedLastLoginDate !== null) {

            if (record.lastLoginIPAdress !== null) {
                return '<table class="table table-sm m-0"><thead><tr><th scope="col" style="width:40%;">Last Login</th><th scope="col" style="width:50%;">IP Address</th><th scope="col" style="width:10%; text-align: center;"><i class="fa fa-cogs" aria-hidden="true"></i></th></tr></thead>' +
                    '<tbody><tr><th scope="row"><i class="fa fa-key" aria-hidden="true"></i> ' + record.formattedLastLoginDate + '</th><td><i class="fa fa-desktop" aria-hidden="true"></i> ' + record.lastLoginIPAdress + '</td><td style="text-align:center;">' +
                    '<i class="fa fa-check-circle text-success"></i>' +
                    '[<a href="#userLoginHistoryInfoModal" id="btnLoginDetailsforUser">1</a>]<input type="hidden" id="userId" value="' + record.id + '"><input type="hidden" id="userName3" value="' + record.userName + '"></td></tr></tbody></table> ';
            }
            else {
                return '<table class="table table-sm m-0"><thead><tr><th scope="col" style="width:40%;">Last Login</th><th scope="col" style="width:50%;">IP Address</th><th scope="col" style="width:10%; text-align: center;"><i class="fa fa-cogs" aria-hidden="true"></i></th></tr></thead>' +
                    '<tbody><tr><th scope="row"><i class="fa fa-key" aria-hidden="true"></i> ' + record.formattedLastLoginDate + '</th><td></td><td style="text-align:center;">' +
                    '<i class="fa fa-check-circle text-success"></i>' +
                    '[<a href="#userLoginHistoryInfoModal" id="btnLoginDetailsforUser">1</a>]<input type="hidden" id="userId" value="' + record.id + '"><input type="hidden" id="userName3" value="' + record.userName + '"></td></tr></tbody></table> ';
            }
        }
    }

    $('#user_settings tbody').on('click', '#btnLoginDetailsforUser', function () {

        $('#userLoginHistoryInfoModal').modal('show');
        var userId = $(this).closest("td").find('#userId').val();
        var userName = $(this).closest("td").find('#userName3').val();
        $('#userLoginHistoryInfoModal').find('#userName2').text(userName)
        fetchUserLoginDetails(userId);
    });


    function fetchUserLoginDetails(userId) {
        $.ajax({
            url: '/Admin/Settings?handler=UserLoginHistory',
            method: 'GET',
            data: { userId: userId },
            success: function (data) {
                // Destroy the existing DataTable instance if it exists
                if ($.fn.DataTable.isDataTable('#UserLoginHistoryInfoDetails')) {
                    $('#UserLoginHistoryInfoDetails').DataTable().destroy();
                }

                $('#UserLoginHistoryInfoDetails').DataTable({
                    autoWidth: false,
                    ordering: false,
                    searching: false,
                    paging: true,
                    info: true,
                    data: data, // Use the data fetched from the AJAX request
                    processing: true,  // Show "Processing..." message while loading data
                    columns: [
                        { data: 'formattedLastLoginDate', width: '22%' },
                        { data: 'guard', width: '20%' },
                        { data: 'siteName', width: '43%' },
                        { data: 'ipAddress', width: '15%' },
                    ]
                });
            },
            error: function (xhr, status, error) {
                console.error('Error fetching data:', error);
            }
        });
    }

    $('#btnSearchUsers').on('click', function () {
        var searchTerm = $('#search_users').val();
        gridUsers.reload({ searchTerm: searchTerm });
    });
    $('#search_users').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridUsers.reload({ searchTerm: $(this).val() });
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
            'data-udeleted="' + record.isDeleted + '" data-action="editUser"><i class="fa fa-pencil"></i></button>';

        if (record.isDeleted) {
            userButtonHtml += '<button class="btn btn-outline-success activateuser" data-user-id="' + record.id + '""> <i class="fa fa-check " aria-hidden="true"></i></button>';
        } else {
            userButtonHtml += '<button class="btn btn-outline-danger deleteuser" data-user-id="' + record.id + '""> <i class="fa fa-trash" aria-hidden="true"></i></button>';
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
            { title: '3rd Party', width: 50, renderer: domainSettings1, cssClass: 'text-center' },
            { width: 100, tmpl: '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal" data-id="{id}"><i class="fa fa-pencil mr-2"></i>Edit</button>', align: 'center' },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    function domainSettings1(value, record) {

        // Check if the row is a newly added row by ID or another condition
        if (record.id === -1) {
            // Skip rendering the checkbox for new rows
            return '';
        }

        if (record.thirdParty != null) {

            return '<input type="checkbox" checked><input type="hidden" id="typeId1" value="' + record.id + '"> <input type="hidden" id="typeName1" value="' + record.name + '">'

        }
        else {
            return '<input type="checkbox"><input type="hidden" id="typeId1" value="' + record.id + '"> <input type="hidden" id="typeName1" value="' + record.name + '">'
        }

    }
    /*p1-248 search client site access grid*/

    $('#btnSearchClientSiteAccess').on('click', function () {
        var searchTerm = $('#search_site_access_control').val();
        console.log('Search Term:', searchTerm);  // Debugging output
        gridClientSiteAccess.reload({ searchTerm: searchTerm });
    });
    $('#search_site_access_control').on('keyup', function (event) {
        // Enter key pressed
        if (event.keyCode === 13) {
            gridClientSiteAccess.reload({ searchTerm: $(this).val() });
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

       
        //3rd Party Dropdown start
        $('#sel_ThirdParty_type').html('');
        $.ajax({
            url: '/Admin/Settings?handler=ClientTypesThirdParty',
            data: {
                UserID: userId
            },
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            $('#sel_ThirdParty_type').append('<option value="">Select</option>');
            data.map(function (clientType) {
                $('#sel_ThirdParty_type').append('<option value="' + clientType.id + '">' + clientType.name + '</option>');
            });
            $.get(`/Admin/Settings?handler=ClientAccessThirdParty&userId=${userId}`, function (data1) {
                if (data1 && data1.thirdPartyID) {
                    console.log(data1.thirdPartyID);
                    $('#sel_ThirdParty_type').val(data1.thirdPartyID); // Set dropdown value
                }
            });
        });
         //3rd Party Dropdown stop
       
        ucaTree.uncheckAll();
        ucaTree.reload({ userId: userId });
    });

    $('#btnSaveUserAccess').on('click', function () {
        if (ucaTree) {
            const userId = $('#user-access-for-id').val();
            var ClientTypeID = $('#sel_ThirdParty_type').val();
            let selectedSites = ucaTree.getCheckedNodes().filter(function (item) {
                return item !== 'undefined';
            });
            $.ajax({
                url: '/Admin/Settings?handler=ClientAccessByUserId',
                data: {
                    userId: userId,
                    selectedSites: selectedSites,
                    ClientTypeID: ClientTypeID
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
            $('#FeedbackTemplate_BackgroundColour').val('#FFFFFF');
            $('#FeedbackTemplate_TextColor').val('#000000');
            $('#FeedbackTemplate_SendtoRC').prop('checked', false);
            $.ajax({
                url: '/Admin/Settings?handler=FeedbackTemplate',
                type: 'GET',
                dataType: 'json',
                data: { templateId: selfeedback }
            }).done(function (data) {
                $('#FeedbackTemplate_Text').val(data.text);
                $('#FeedbackTemplate_Type').val(data.type);
                $('#FeedbackTemplate_BackgroundColour').val(data.backgroundColour);
                $('#FeedbackTemplate_TextColor').val(data.textColor);
                $('#FeedbackTemplate_SendtoRC').prop('checked', data.sendtoRC);
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
        $('#FeedbackTemplate_BackgroundColour').val('#FFFFFF');
        $('#FeedbackTemplate_TextColor').val('#000000');
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
        $('#FeedbackTemplate_BackgroundColour').val('#FFFFFF');
        $('#FeedbackTemplate_TextColor').val('#000000');
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

    $(".edit-btn").click(function () {
        var id = $(this).data("id");
        $("#DefaultMail_" + id).hide();
        $("#DefaultMailTextbox_" + id).show();
        $(this).hide();
        $(".update-btn[data-id='" + id + "']").show();
    });

    $(".update-btn").click(function () {
        var id = $(this).data("id");
        var domain = $(this).data("domain");
        var newEmail = $("#DefaultMailTextboxval_" + id).val();
        var defaultMailEdit = newEmail;
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(defaultMailEdit)) {

            showStatusNotification(false, 'Please enter a valid email address.');
            return;
        }

        $.ajax({
            url: '/Admin/Settings?handler=DefaultEmailUpdateThirdPartyDomains',
            type: 'POST',
            data: { domainId: id, domain: domain, DefaultEmail: newEmail }, // Send data as key-value pair
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (data) {
            console.log("Response Data:", data); // Debugging: See what data is returned

            if (data.success) {
                showStatusNotification(true, data.message);

                // Update the Last Updated Date
                if (data.dateTimeUpdated) {
                    $("#ir_template_updatedOn_" + id).text(data.dateTimeUpdated);
                }

                // Update the Email Text
                $("#DefaultMail_" + id).text(data.defaultEmail || "-").show();
                $("#DefaultMailTextbox_" + id).hide();

                // Hide update button, show edit button
                $(".update-btn[data-id='" + id + "']").hide();
                $(".edit-btn[data-id='" + id + "']").show();

                // Clear file input if necessary
                $("#row_" + id + " .file-upload").val("");

            } else {
                showStatusNotification(false, data.message || 'Something went wrong');
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        });

       
    });

    $(".file-upload").change(function () {
        const file = $(this).get(0).files.item(0);
        if (!file) return; // Prevent errors if no file is selected
        const fileExtn = file.name.split('.').pop().toLowerCase();
        if (fileExtn !== 'pdf') {
            showModal('Unsupported file type. Please upload a .pdf file');
            return false;
        }
        const fileForm = new FormData();
        fileForm.append('file', file);
        // Retrieve additional data from the element
        const id = $(this).data("id");
        const domain = $(this).data("domain");
        fileForm.append('domainId', id);
        fileForm.append('domain', domain);

        $.ajax({
            url: '/Admin/Settings?handler=IrTemplateUploadThirdParty',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {
                showStatusNotification(true, data.message);

                // Update the Last Updated Date
                if (data.dateTimeUpdated) {
                    $("#ir_template_updatedOn_" + id).text(data.dateTimeUpdated);
                }

                // Update the Email Text
                $("#DefaultMail_" + id).text(data.defaultEmail || "-").show();
                $("#DefaultMailTextbox_" + id).hide();

                // Hide update button, show edit button
                $(".update-btn[data-id='" + id + "']").hide();
                $(".edit-btn[data-id='" + id + "']").show();

                // Clear file input if necessary
                $("#row_" + id + " .file-upload").val("");

                // Update the Download button dynamically
                if (data.filename) {
                    let downloadButton = $("#download_btn_" + id);
                    downloadButton.attr("href", `/Pdf/Template/${data.filename}`); // Update file path
                    downloadButton.removeClass("disabled"); // Enable button
                }

            } else {
                showStatusNotification(false, data.message || 'Something went wrong');
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#ir_template_upload').val(''); // Reset input field
        });
    });

    // Update row function (refresh only the affected row)
    function updateRow(domainId, data) {
        $("#ir_template_updatedOn_" + domainId).text(data.dateTimeUpdated);
        $("#DefaultMail_" + domainId).text(data.defaultEmail || "-");
        $("#row_" + domainId + " .file-upload").val(""); // Clear file input
    }

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
    /* Grid for CompanySop Start*/

    var editManagerstaffDocsButtonRendererCompanySop;
    editManagerstaffDocsButtonRendererCompanySop = function (value, record, $cell, $displayEl, id, $grid) {
        var data = $grid.data(),
            $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_company_sop" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
                '<input type="file" name="upload_staff_file_company_sop" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
                '</form></label>').attr('data-key', id),
            $downlaod = $('<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
            $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
            $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_company_sop" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
            $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
            $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
        $edit.on('click', function (e) {
            $grid.edit($(this).data('key'));
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        });
        $delete.on('click', function (e) {
            $grid.removeRow($(this).data('key'));
        });
        $update.on('click', function (e) {
            $grid.update($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $cancel.on('click', function (e) {
            $grid.cancel($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $displayEl.empty().append($replace).append($downlaod).append($edit).append($delete).append($update).append($cancel);
    }
    gridStaffDocsTypeCompanySop = $('#staff_document_files_type_CompanySop').grid({
        //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=1',
        dataSource: {
            url: '/Admin/Settings?handler=StaffDocsUsingType&&type=1',
            data: function () {
                return { query: $('#searchBoxTempAndForms').val() }; // Include query dynamically
            }
        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command', managementColumn: false },
        columns: [
            { field: 'fileName', title: 'File Name', width: 390 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
            { width: 75, field: 'documentModuleName', title: '?', align: 'center', type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=HelpDocValues', valueField: 'name', textField: 'name' } },
            // { width: 200, renderer: staffDocsButtonRendererCompanySop },
            { width: 270, renderer: editManagerstaffDocsButtonRendererCompanySop },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridStaffDocsTypeCompanySop) {
        gridStaffDocsTypeCompanySop.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=UpdateDocumentModuleType',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {

                if (result.status) {
                    showStatusNotification(true, 'Updated Successfully');
                    gridStaffDocsTypeCompanySop.clear();
                    gridStaffDocsTypeCompanySop.reload();
                } else {

                    showStatusNotification(false, 'Please try again');
                    gridStaffDocsTypeCompanySop.edit(id);
                }
            }).fail(function () {
                console.log('error');
            }).always(function () {

            });
        });

    }
    /* Grid for CompanySop end*/
    /**************/
    /* Grid for Training*/
    /*StaffDocs start */
    var editManagerstaffDocsButtonRendererTraining;
    editManagerstaffDocsButtonRendererTraining = function (value, record, $cell, $displayEl, id, $grid) {
        var data = $grid.data(),
            $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_training" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
                '<input type="file" name="upload_staff_file_training" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
                '</form></label>').attr('data-key', id),
            $downlaod = $('<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
            $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
            $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_training" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
            $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
            $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
        $edit.on('click', function (e) {
            $grid.edit($(this).data('key'));
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        });
        $delete.on('click', function (e) {
            $grid.removeRow($(this).data('key'));
        });
        $update.on('click', function (e) {
            $grid.update($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $cancel.on('click', function (e) {
            $grid.cancel($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $displayEl.empty().append($replace).append($downlaod).append($edit).append($delete).append($update).append($cancel);
    }
    gridStaffDocsTypeTraining = $('#staff_document_files_type_Training').grid({
        //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=2',
        dataSource: {
            url: '/Admin/Settings?handler=StaffDocsUsingType&&type=2',
            data: function () {
                return { query: $('#searchBoxTempAndForms').val() }; // Include query dynamically
            }
        },

        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        inlineEditing: { mode: 'command', managementColumn: false },
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 390 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
            { width: 75, field: 'documentModuleName', title: '?', align: 'center', type: 'dropdown', editor: { dataSource: '/Admin/Settings?handler=HelpDocValues', valueField: 'name', textField: 'name' } },
            //{ width: 200, renderer: staffDocsButtonRendererTraining },
            { width: 270, align: 'center', renderer: editManagerstaffDocsButtonRendererTraining }
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    /*update document module type*/
    if (gridStaffDocsTypeTraining) {
        gridStaffDocsTypeTraining.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=UpdateDocumentModuleType',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {

                if (result.status) {
                    showStatusNotification(true, 'Updated Successfully');
                    gridStaffDocsTypeTraining.clear();
                    gridStaffDocsTypeTraining.reload();
                } else {

                    showStatusNotification(false, 'Please try again');
                    gridStaffDocsTypeTraining.edit(id);
                }
            }).fail(function () {
                console.log('error');
            }).always(function () {

            });
        });

    }
    /*StaffDocs start end */

    //Staff forms start
    var editManagerstaffDocsButtonRendererFroms;
    editManagerstaffDocsButtonRendererFroms = function (value, record, $cell, $displayEl, id, $grid) {
        var data = $grid.data(),
            $replace = $('<label class="btn btn-success mb-0"><form id="form_file_downloads_templates_forms" method="post"><i class="fa fa-upload mr-2"></i>Replace' +
                '<input type="file" name="upload_staff_file_templates_forms" accept=".pdf, .docx, .xlsx" hidden data-doc-id="' + record.id + '">' +
                '</form></label>').attr('data-key', id),
            $downlaod = $('<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a>').attr('data-key', id),
            $edit = $('<button class="btn btn-outline-primary ml-2"><i class="gj-icon pencil" style="font-size:15px"></i></button>').attr('data-key', id),
            $delete = $('<button type="button" class="btn btn-outline-danger ml-2 delete_staff_file_templates_forms" data-doc-id="' + record.id + '"><i class="fa fa-trash"></i></button>').attr('data-key', id),
            $update = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-check" aria-hidden="true"></i></button>').attr('data-key', id).hide(),
            $cancel = $('<button class="btn btn-outline-primary ml-2"><i class="fa fa-close" aria-hidden="true"></i></button>').attr('data-key', id).hide();
        $edit.on('click', function (e) {
            $grid.edit($(this).data('key'));
            $edit.hide();
            $delete.hide();
            $update.show();
            $cancel.show();
        });
        $delete.on('click', function (e) {
            $grid.removeRow($(this).data('key'));
        });
        $update.on('click', function (e) {
            $grid.update($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $cancel.on('click', function (e) {
            $grid.cancel($(this).data('key'));
            $edit.show();
            $delete.show();
            $update.hide();
            $cancel.hide();
        });
        $displayEl.empty().append($replace).append($downlaod).append($edit).append($delete).append($update).append($cancel);
    }
    gridStaffDocsTypeTemplatesAndForms = $('#staff_document_files_type_TemplatesAndForms').grid({
        dataSource: {
            url: '/Admin/Settings?handler=StaffDocsUsingType&&type=3',
            data: function () {
                return { query: $('#searchBoxTempAndForms').val() }; // Include query dynamically
            }
        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        inlineEditing: { mode: 'command', managementColumn: false },
        primaryKey: 'id',
        columns: [
            { field: 'fileName', title: 'File Name', width: 390 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 140 },
            { width: 75, field: 'documentModuleName', title: '?', align: 'center', type: 'dropdown', editField: 'documentModuleName', editor: { dataSource: '/Admin/Settings?handler=HelpDocValues', valueField: 'name', textField: 'name' } },

            { width: 270, align: 'center', renderer: editManagerstaffDocsButtonRendererFroms }
            //{ width: 200, renderer: staffDocsButtonRendererTemplatesAndForms },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    if (gridStaffDocsTypeTemplatesAndForms) {
        gridStaffDocsTypeTemplatesAndForms.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const token = $('input[name="__RequestVerificationToken"]').val();
            $.ajax({
                url: '/Admin/Settings?handler=UpdateDocumentModuleType',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function (result) {

                if (result.status) {
                    showStatusNotification(true, 'Updated Successfully');
                    gridStaffDocsTypeTemplatesAndForms.clear();
                    gridStaffDocsTypeTemplatesAndForms.reload();
                } else {

                    showStatusNotification(false, 'Please try again');
                    gridStaffDocsTypeTemplatesAndForms.edit(id);
                }
            }).fail(function () {
                console.log('error');
            }).always(function () {

            });
        });

    }



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
    /*$('#add_staff_document_file_SlientSite_sop').on('change', function () {
         uploadStafDocUsingTypeTypeFour($(this), false, 4);
     });*/





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
        var check = $('#SOP').val();
        var check2 = $('#clientSitessiteSOP').val();
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




    function uploadStafDocUsingTypeTypeFour(uploadCtrl, edit = false, type) {
        var Email = $('#file_downloads').val();
        var sop = $('#SOP').val();
        var site = $('#clientSitessiteSOP').val();

        /*if (sop == '') {

            $('#add_staff_document_file_SlientSite_sop').val('');
                showModal('Please select SOP');
                return false;
            
        }*/
        if (site == '') {

            $('#add_staff_document_file_SlientSite_sop').val('');
            showModal('Please select Site');
            return false;

        }


        const file = uploadCtrl.get(0).files.item(0);
        const fileExtn = file.name.split('.').pop();
        if (!fileExtn || '.pdf,.docx,.xlsx'.indexOf(fileExtn.toLowerCase()) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx or .xlsx file');
            return false;
        }

        const fileForm = new FormData();
        fileForm.append('file', file);
        fileForm.append('type', type);
        fileForm.append('sop', sop);
        fileForm.append('site', site);
        if (edit)
            fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDocUsingTypeFour',
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
        return '<div class="button-container-div">' +
            '<button id="btnSopDownload" data-sop-filename="' + record.fileName + '" class="btn btn-outline-primary ml-2"><i class="fa fa-download mr-2"></i>Download</button>' +
            '</div>'
    }

    $('#mdlAuthGuardForSopDownload').on('show.bs.modal', function (event) {
        $('#GuardDownloadSop_SecurityNo').val('');
    });
    $('#mdlAuthGuardForSopDownload').on('hide.bs.modal', function (event) {
        $('#sop_filename').val('');
    });

    $('#file_downloads tbody').on('click', '#btnSopDownload', function () {
        const btn = $(this);
        $('#sop_filename').val(btn.attr('data-sop-filename'));
        $('#AuthGuardForSopDwnldValidationSummary').html('');
        $('#mdlAuthGuardForSopDownload').modal('show');
    });

    $('#btnAuthGuardForSopDwnld').on('click', function () {
        $('#AuthGuardForSopDwnldValidationSummary').html('');
        const btn = $(this);
        var filename_todownload = $('#sop_filename').val();
        var Catg_todownload = $('#sop_catg_type').val();

        $('#loader').show();

        var tmdata = {
            'EventDateTimeLocal': null,
            'EventDateTimeLocalWithOffset': null,
            'EventDateTimeZone': null,
            'EventDateTimeZoneShort': null,
            'EventDateTimeUtcOffsetMinute': null,
        };

        /*fillRefreshLocalTimeZoneDetails(tmdata, "", false)*/

        var DateTime = luxon.DateTime;
        var dt1 = DateTime.local();
        let tz = dt1.zoneName + ' ' + dt1.offsetNameShort;
        let diffTZ = dt1.offset
        let tzshrtnm = 'GMT' + dt1.toFormat('ZZ');
        const eventDateTimeLocal = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS');
        const eventDateTimeLocalWithOffset = dt1.toFormat('yyyy-MM-dd HH:mm:ss.SSS Z');
        tmdata.EventDateTimeLocal = eventDateTimeLocal;
        tmdata.EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset;
        tmdata.EventDateTimeZone = tz;
        tmdata.EventDateTimeZoneShort = tzshrtnm;
        tmdata.EventDateTimeUtcOffsetMinute = diffTZ;

        $.ajax({
            url: '/Incident/Downloads?handler=CheckAndCreateDownloadAuditLog',
            type: 'POST',
            data: {
                guardLicNo: $('#GuardDownloadSop_SecurityNo').val(),
                downloadCatg: Catg_todownload,
                downloadFileName: filename_todownload,
                tmdata: tmdata
            },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) {
                $('#mdlAuthGuardForSopDownload').modal('hide');
                // '<div class="button-container-div"><a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary ml-2" target="_blank"><i class="fa fa-download mr-2"></i>Download</a></div>'
                //var a = document.createElement('a');
                //a.href = "/StaffDocs/" + filename_todownload;
                //a.target = "_blank";
                //a.click();
                //a.remove();
                var URL = "/StaffDocs/" + encodeURIComponent(filename_todownload);
                window.open(URL, "_blank")
            }
            else {
                $('#AuthGuardForSopDwnldValidationSummary').html(result.message);
                // alert(result.message);
            }
        }).always(function () {
            $('#loader').hide();
        });

    });


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

    /*****IR Email CC for Reimbursement  *****/



    function getIrEmailCCforReimbursement() {


        $.ajax({
            url: '/Admin/Settings?handler=IREmailCCForReimbursements',
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {

            for (var i = 0; i < data.length; i++) {
                $('#txt_IrEmailCCForReimbursements').val(data[i].name);

            }

        });
    }

    //save btn IR Email-Click -start 

    $('#btn_add_FR_settings').on('click', function () {
        const token = $('input[name="__RequestVerificationToken"]').val();
        var Email = $('#txt_IrEmailCCForReimbursements').val();
        //var emailsArray = Email.split(',');
        //for (var i = 0; i < emailsArray.length; i++) {
        //var emailAddress = emailsArray[i].trim();
        var emailAddress = Email
        if (isValidEmail(emailAddress)) {
            $.ajax({
                url: '/Admin/Settings?handler=SaveIREmail',
                data: { Email: emailAddress },
                type: 'POST',
                headers: { 'RequestVerificationToken': token },
            }).done(function () {
            })
        }
        else {
            $.notify("Invalid email address.",
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );

        }

        //}

        function isValidEmail(email) {
            // Regular expression for basic email validation
            var emailPattern = /^(?:[^,\s@]+@[^,\s@]+\.[^,\s@]+(?:,\s*)?)+$/;
            return emailPattern.test(email);
        }
    })

    //save btn IR Email-Click -end 


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
                //p1-225 Core Settings-start
                $("#txt_HyplerLinkLabel").val(data[i].hyperlinkLabel);
                $("#txt_HyperlinkColor").val(data[i].hyperlinkColour);
                $("#txt_LogoHyplerLink").val(data[i].logoHyperlink);
                $("#txt_APIProvider").val(data[i].apiProvider);
                $("#txt_APIsecretkey").val(data[i].apiSecretkey)

                //p1-225 Core Settings-end
                $("#txt_KPI").val(data[i].kpiMail);
                $("#txt_IR").val(data[i].irMail);
                $("#txt_Fusion").val(data[i].fusionMail);
                $("#txt_Timesheet").val(data[i].timesheetsMail);
                $("#txt_APIProviderForIR").val(data[i].apiProviderIR);
                $("#txt_APIsecretkeyForIR").val(data[i].apiSecretkeyIR)

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
                EmailMessage: $("#txt_EmailMessage").val(),
                //p1-225 Core Settings-start
                HyperlinkLabel: $("#txt_HyplerLinkLabel").val(),
                HyperlinkColour: $("#txt_HyperlinkColor").val(),
                LogoHyperlink: $("#txt_LogoHyplerLink").val(),
                
                //p1-225 Core Settings-end
                
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


    $("#btn_CompanyMailSave").on("click", function () {
        var companyId = parseInt($("#txt_CompanyId").val());
        const token = $('input[name="__RequestVerificationToken"]').val();
        var ss = $("#txt_Timesheet").val();
        var obj = {
            Id: companyId,
            IRMail: $("#txt_IR").val(),
            KPIMail: $("#txt_KPI").val(),
            FusionMail: $("#txt_Fusion").val(),
            TimesheetsMail: $("#txt_Timesheet").val(),
        };

        $.ajax({
            url: '/Admin/Settings?handler=CompanyMailDetails',
            data: { 'company': obj },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            if (result.status) {
                if (result.message !== '') {
                    getC4Settings();
                    showStatusNotification(true, 'Mail details modified successfully');
                }
            } else {
                displayGuardValidationSummary(result.message);
            }
        }).fail(function () {
            alert("An error occurred while saving Mail details.");
        });

        // Prevent any default action (like a form submission) that might reload the page
        return false;
    });
    $("#btn_CompanyAPISave").on("click", function () {
        var companyId = parseInt($("#txt_CompanyId").val());
        const token = $('input[name="__RequestVerificationToken"]').val();
        var ss = $("#txt_Timesheet").val();
        var obj = {
            Id: companyId,
            ApiProvider: $("#txt_APIProvider").val(),
            ApiSecretkey: $("#txt_APIsecretkey").val(),
            ApiProviderIR: $("#txt_APIProviderForIR").val(),
            ApiSecretkeyIR: $("#txt_APIsecretkeyForIR").val(),
        };

        $.ajax({
            url: '/Admin/Settings?handler=CompanyAPIDetails',
            data: { 'company': obj },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (result) {
            if (result.status) {
                if (result.message !== '') {
                    getC4Settings();
                    showStatusNotification(true, 'API details modified successfully');
                }
            } else {
                displayGuardValidationSummary(result.message);
            }
        }).fail(function () {
            alert("An error occurred while saving Mail details.");
        });

        // Prevent any default action (like a form submission) that might reload the page
        return false;
    });

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

    $('.marqueeurls').on('click', function () {
        var urlstr = $('#inp_marqueeurls').val();
        if (urlstr != '') {
            var url_lst = urlstr.split('|');
            var alnk = document.getElementById('a_marqueeurls');
            url_lst.forEach(function (item) {
                console.log(item);
                if (item != '') {
                    alnk.removeAttribute("href");
                    alnk.setAttribute("href", item);
                    alnk.click();
                }
            });
        }
    });

    //p1 - 196 Rationalization Of Menu Changes - start
    let gridKvlFields;
    gridKvlFields = $('#tbl_kvl_fields').grid({
        dataSource: '/Admin/GuardSettings?handler=KeyVehcileLogFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [
            { field: 'name', title: 'Name', width: 200, editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    let isKvlFieldAdding = false;
    $('#add_kvl_fields').on('click', function () {
        const selFieldTypeId = $('#kvl_fields_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        var rowCount = $('#tbl_kvl_fields tr').length;

        if (selFieldTypeId == '9' && rowCount == 8) {
            alert('Maximum number of CRM/BDM Activity exeeded');
            return;
        }

        if (isKvlFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isKvlFieldAdding = true;
            gridKvlFields.addRow({
                'id': -1,
                'typeId': selFieldTypeId,
                'name': '',
            }).edit(-1);
        }
    });

    $('#kvl_fields_types').on('change', function () {
        const selKvlFieldTypeId = $('#kvl_fields_types').val();
        gridKvlFields.clear();
        gridKvlFields.reload({ typeId: selKvlFieldTypeId });
    });

    if (gridKvlFields) {
        gridKvlFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveKeyVehicleLogField',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isKvlFieldAdding)
                    isKvlFieldAdding = false;
            });
        });

        gridKvlFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteKeyVehicleLogField',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isKvlFieldAdding)
                        isKvlFieldAdding = false;
                });
            }
        });
    }
    /*to add do's and donts -start*/


    let gridDosAndDontsFields;
    let isDosandDontsFieldAdding = false;
    gridDosAndDontsFields = $('#tbl_dosanddonts_fields').grid({
        dataSource: '/Admin/GuardSettings?handler=DosandDontsFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [

            { field: 'referenceNo', title: 'REF NO', width: 100, editor: true },
            { field: 'name', title: 'Name', width: 400, editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    $('#doanddontfields_types').on('change', function () {
        const selKvlFieldTypeId = $('#doanddontfields_types').val();
        gridDosAndDontsFields.clear();
        gridDosAndDontsFields.reload({ typeId: selKvlFieldTypeId });
    });
    //p5 - Issue - 20 - Instructor - start
    let gridTAFields;
    let isTAFieldAdding = false;
    gridTAFields = $('#tbl_ta_fields').grid({
        dataSource: '/Admin/GuardSettings?handler=TrainingInstructorNameandPositionFields',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [

            { field: 'name', title: 'Instructor Name', width: '100%', editor: true },
            { field: 'position', title: 'Instructor Position', width: '100%', editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });
    //p5 - Issue - 20 - Instructor - end
    $('#add_dosanddonts_fields').on('click', function () {
        const selFieldTypeId = $('#doanddontfields_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        var rowCount = $('#tbl_dosanddonts_fields tr').length;


        if (isDosandDontsFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isKvlFieldAdding = true;
            gridDosAndDontsFields.addRow({
                'id': -1,
                'typeId': selFieldTypeId,
                'name': '',
            }).edit(-1);
        }
    });
    //p5 - Issue - 20 - Instructor - start
    $('#add_ta_fields').on('click', function () {
        const selFieldTypeId = $('#ta_field_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        var rowCount = $('#tbl_ta_fields tr').length;


        if (isTAFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isTAFieldAdding = true;
            gridTAFields.addRow({
                'id': -1,
                'name': '',
                'position': '',
            }).edit(-1);
        }
    });
    //p5 - Issue - 20 - Instructor - end
    if (gridDosAndDontsFields) {
        gridDosAndDontsFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            if (isNaN(data.referenceNo)) {
                $.notify('Reference number should only contains numbers. !!!',
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#D44950",
                        blur: 0.4,
                        delay: 0
                    }
                );
                gridDosAndDontsFields.edit(id);
                return;
            }
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveDosandDontsField',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) {
                    //$.notify(result.message,
                    //    {
                    //        align: "center",
                    //        verticalAlign: "top",
                    //        color: "#fff",
                    //        background: "#20D67B",
                    //        blur: 0.4,
                    //        delay: 0
                    //    }
                    //);

                    gridDosAndDontsFields.reload({ typeId: $('#doanddontfields_types').val() });
                }
                else {
                    alert(result.message);
                    //$.notify(result.message,
                    //    {
                    //        align: "center",
                    //        verticalAlign: "top",
                    //        color: "#fff",
                    //        background: "#20D67B",
                    //        blur: 0.4,
                    //        delay: 0
                    //    }
                    //);
                    gridDosAndDontsFields.edit(id);

                }
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isDosandDontsFieldAdding)
                    isDosandDontsFieldAdding = false;
            });
        });

        gridDosAndDontsFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteDosandDontsField',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridDosAndDontsFields.reload({ typeId: $('#doanddontfields_types').val() });
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isKvlFieldAdding)
                        isKvlFieldAdding = false;
                });
            }
        });
    }
    /*to add do's and donts -end*/
    /*to add KPI Telematics -start*/

    let gridKPITelematicsFields;
    let isKPITelematicsFieldAdding = false;
    gridKPITelematicsFields = $('#tbl_kpitelematics_fields').grid({
        dataSource: '/Admin/GuardSettings?handler=KPITelematics',
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        inlineEditing: { mode: 'command' },
        columns: [

            { field: 'name', title: 'Name', width: 300, editor: true },
            { field: 'mobile', title: 'Mobile', width: 150, editor: true },
            { field: 'email', title: 'Email', width: 310, editor: true },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>').css('width', '200px');
           // $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    $('#KPITelematicsfields_types').on('change', function () {
        const selKvlFieldTypeId = $('#KPITelematicsfields_types').val();
        gridKPITelematicsFields.clear();
        gridKPITelematicsFields.reload({ typeId: selKvlFieldTypeId });
    });

    //$('#add_KPI_Telematics_fields').on('click', function () {
    //    const selFieldTypeId = $('#KPITelematicsfields_types').val();
    //    if (!selFieldTypeId) {
    //        alert('Please select a field type to update');
    //        return;
    //    }
    //    var rowCount = $('#tbl_kpitelematics_fields tr').length;


    //    if (isKPITelematicsFieldAdding) {
    //        alert('Unsaved changes in the grid. Refresh the page');
    //    } else {
    //        isKvlFieldAdding = true;
    //        gridKPITelematicsFields.addRow({
    //            'id': -1,
    //            'typeId': selFieldTypeId,
    //            'name': '',
    //        }).edit(-1);
    //    }
    //});

    $('#add_KPI_Telematics_fields').on('click', function () {
        const selFieldTypeId = $('#KPITelematicsfields_types').val();
        if (!selFieldTypeId) {
            alert('Please select a field type to update');
            return;
        }
        var rowCount = $('#tbl_kpitelematics_fields tr').length;


        if (isKPITelematicsFieldAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isKvlFieldAdding = true;
            gridKPITelematicsFields.addRow({
                'id': -1,
                'typeId': selFieldTypeId,
                'name': '',
            }).edit(-1);
        }
    });
    if (gridKPITelematicsFields) {
        gridKPITelematicsFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const mobileRegex = /^\d{10}$/; // Example for a 10-digit number

            let isValid = true;
            let errorMessage = "";

            if (!emailRegex.test(data.email)) {
                isValid = false;
                errorMessage += "Invalid email format.\n";
            }


            if (!isValid) {
                alert(errorMessage);
                return; // Stop further execution
            }

            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveKPITelematics',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) {

                    //$.notify(result.message,
                    //    {
                    //        align: "center",
                    //        verticalAlign: "top",
                    //        color: "#fff",
                    //        background: "#20D67B",
                    //        blur: 0.4,
                    //        delay: 0
                    //    }
                    //);

                    gridKPITelematicsFields.reload({ typeId: $('#KPITelematicsfields_types').val() });
                }
                else {
                    alert(result.message);
                    //$.notify(result.message,
                    //    {
                    //        align: "center",
                    //        verticalAlign: "top",
                    //        color: "#fff",
                    //        background: "#20D67B",
                    //        blur: 0.4,
                    //        delay: 0
                    //    }
                    //);
                    gridKPITelematicsFields.edit(id);


                }

            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isKPITelematicsFieldAdding)
                    isKPITelematicsFieldAdding = false;
            });
        });

        gridKPITelematicsFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteKPITelematics',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) gridKPITelematicsFields.reload({ typeId: $('#KPITelematicsfields_types').val() });
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isKPITelematicsFieldAdding)
                        isKPITelematicsFieldAdding = false;
                });
            }
        });

    }

    //p5 - Issue - 20 - Instructor - start
    if (gridTAFields) {
        gridTAFields.on('rowDataChanged', function (e, id, record) {
            const data = $.extend(true, {}, record);
            if (data.name == '' || data.name == null) {
                $.notify('Instructor Name should not be empty. !!!',
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#D44950",
                        blur: 0.4,
                        delay: 0
                    }
                );
                gridTAFields.edit(id);
                return;
            }
            if (data.position == '' || data.position == null) {
                $.notify('Instructor Position should not be empty. !!!',
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#D44950",
                        blur: 0.4,
                        delay: 0
                    }
                );
                gridTAFields.edit(id);
                return;
            }
            $.ajax({
                url: '/Admin/GuardSettings?handler=SaveTrainingInstructorNameandPositionFields',
                data: { record: data },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) {

                    gridTAFields.clear();
                    gridTAFields.reload();
                }
                else {
                    alert(result.message);

                    gridTAFields.edit(id);

                }
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isTAFieldAdding)
                    isTAFieldAdding = false;
            });
        });

        gridTAFields.on('rowRemoving', function (e, id, record) {
            if (confirm('Are you sure want to delete this field?')) {
                $.ajax({
                    url: '/Admin/GuardSettings?handler=DeleteTrainingInstructorNameandPositionFields',
                    data: { id: record },
                    type: 'POST',
                    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                }).done(function (result) {
                    if (result.success) { gridTAFields.clear(); gridTAFields.reload(); }
                    else alert(result.message);
                }).fail(function () {
                    console.log('error');
                }).always(function () {
                    if (isTAFieldAdding)
                        isTAFieldAdding = false;
                });
            }
        });
    }
    //p5 - Issue - 20 - Instructor - end
    if ($('#report_module_types').val() == '') {
        $('#doanddontfields_types').hide();
        $('#report_field_types').hide();
        $('#kvl_fields_types').hide();

        $('#lblFieldType').hide();
        $('#KPITelematicsfields_types').hide();

        $('#add_KPI_Telematics_fields').hide();
        $('#add_field_settings').hide();
        $('#add_dosanddonts_fields').hide();
        $('#add_kvl_fields').hide();
        $('#irNotes').hide();
        gridReportFields.hide();
        gridKvlFields.hide();
        gridDosAndDontsFields.hide();
        gridAreaReportFields.hide();
        gridKPITelematicsFields.hide();
        /*p5-Issue-20-Instructor-start*/
        gridTAFields.hide();
        $('#ta_field_types').hide();
        $('#add_ta_fields').hide();
        $('#duressapp_types').hide();
        gridDuressAppLogFields.hide();
        gridDuressAppAudioFields.hide();
        gridDuressAppMultimediaFields.hide();
        $('#add_duressapp_fields').hide();
        $('#add_DuressAppAudio').hide();
        $('#add_duressappMultimedia_fields').hide();

        //p5 - Issue - 20 - Instructor - end

    }

    $('#report_module_types').on('change', function () {
        if ($('#report_module_types').val() == 1) {

            $('#fieldSettings').show();
            $('#doanddontfields_types').show();
            $('#report_field_types').hide();
            $('#kvl_fields_types').hide();
            $('#KPITelematicsfields_types').hide();
            $('#lblFieldType').show();
            $('#add_KPI_Telematics_fields').hide();
            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').show();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();

            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            gridReportFields.hide();
            gridKvlFields.hide();
            gridAreaReportFields.hide();
            gridDosAndDontsFields.show();
            gridKPITelematicsFields.hide();
            $('#doanddontfields_types').val('');
            gridDosAndDontsFields.reload({ typeId: $('#doanddontfields_types').val() });

            /*p5-Issue-20-Instructor-start*/
            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            /*p5-Issue-20-Instructor-end*/

        }
        else if ($('#report_module_types').val() == 2) {
            $('#fieldSettings').show();
            $('#doanddontfields_types').hide();
            $('#report_field_types').hide();

            $('#kvl_fields_types').show();

            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            $('#lblFieldType').show();
            $('#KPITelematicsfields_types').hide();
            $('#add_KPI_Telematics_fields').hide();
            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').show();
            $('#irNotes').hide();
            $('#kvl_fields_types').val('');
            gridKvlFields.reload({ typeId: $('#kvl_fields_types').val() });

            gridReportFields.hide();
            gridAreaReportFields.hide();
            gridKvlFields.show();
            gridDosAndDontsFields.hide();
            gridKPITelematicsFields.hide();
            /*p5-Issue-20-Instructor-start*/
            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            /*p5-Issue-20-Instructor-end*/

        }
        else if ($('#report_module_types').val() == 3) {
            $('#fieldSettings').hide();
            $('#doanddontfields_types').hide();
            $('#report_field_types').show();
            $('#kvl_fields_types').hide();

            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            $('#lblFieldType').show();
            $('#KPITelematicsfields_types').hide();
            $('#add_KPI_Telematics_fields').hide();
            $('#add_field_settings').show();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();
            gridReportFields.show();
            gridKvlFields.hide();
            gridDosAndDontsFields.hide();
            gridAreaReportFields.hide();
            gridKPITelematicsFields.hide();
            

            $('#report_field_types').val('');
            gridReportFields.reload({ typeId: $('#report_field_types').val() });

            /*p5-Issue-20-Instructor-start*/
            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            /*p5-Issue-20-Instructor-end*/
        }
        else if ($('#report_module_types').val() == 5) {
            $('#fieldSettings').show();
            $('#doanddontfields_types').hide();
            $('#report_field_types').hide();
            $('#kvl_fields_types').hide();

            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            $('#lblFieldType').show();
            $('#KPITelematicsfields_types').show();
            $('#add_KPI_Telematics_fields').show();
            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();
            gridReportFields.hide();
            gridKvlFields.hide();
            gridDosAndDontsFields.hide();
            gridAreaReportFields.hide();
            gridKPITelematicsFields.show();


            $('#report_field_types').val('');
            gridReportFields.reload({ typeId: $('#report_field_types').val() });

            /*p5-Issue-20-Instructor-start*/
            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            /*p5-Issue-20-Instructor-end*/
        }
        else if ($('#report_module_types').val() == 4) {
            $('#fieldSettings').hide();
            $('#doanddontfields_types').hide();
            $('#report_field_types').hide();
            $('#kvl_fields_types').hide();

            $('#lblFieldType').show();
            $('#KPITelematicsfields_types').hide();
            $('#add_KPI_Telematics_fields').hide();



            gridTAFields.hide();
            $('#ta_field_types').show();
            $('#add_ta_fields').show();
            $('#ta_field_types').val('');
            // gridTAFields.reload();

            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();
            gridReportFields.hide();
            gridKvlFields.hide();
            gridDosAndDontsFields.hide();
            gridAreaReportFields.hide();
            gridKPITelematicsFields.hide();           
            $('#report_field_types').val('');
            $('#ta_field_types').show();
            $('#add_ta_fields').show();
            gridTAFields.show();
            gridTAFields.reload({ typeId: $('#ta_field_types').val() });

        }
        else if ($('#report_module_types').val() == 6) {
            $('#fieldSettings').show();
            $('#doanddontfields_types').hide();
            $('#report_field_types').hide();
            $('#kvl_fields_types').hide();

            $('#lblFieldType').show();
            $('#KPITelematicsfields_types').hide();
            $('#add_KPI_Telematics_fields').hide();


            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            $('#ta_field_types').val('');
            // gridTAFields.reload();

            $('#duressapp_types').show();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            gridDuressAppMultimediaFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();
            $('#add_duressappMultimedia_fields').hide();

            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();
            gridReportFields.hide();
            gridKvlFields.hide();
            gridDosAndDontsFields.hide();
            gridAreaReportFields.hide();
            gridKPITelematicsFields.hide();
            $('#report_field_types').val('');
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            gridTAFields.hide();
            gridTAFields.reload({ typeId: $('#ta_field_types').val() });

        }
        else {

            $('#doanddontfields_types').hide();
            $('#report_field_types').hide();
            $('#kvl_fields_types').hide();
            $('#KPITelematicsfields_types').hide();
            $('#lblFieldType').hide();


            $('#duressapp_types').hide();
            gridDuressAppLogFields.hide();
            gridDuressAppAudioFields.hide();
            $('#add_duressapp_fields').hide();
            $('#add_DuressAppAudio').hide();

            $('#add_field_settings').hide();
            $('#add_dosanddonts_fields').hide();
            $('#add_kvl_fields').hide();
            $('#irNotes').hide();
            gridAreaReportFields.hide();
            gridReportFields.hide();
            gridKvlFields.hide();
            gridDosAndDontsFields.hide();
            gridKPITelematicsFields.hide();         
            /*p5-Issue-20-Instructor-start*/
            gridTAFields.hide();
            $('#ta_field_types').hide();
            $('#add_ta_fields').hide();
            /*p5-Issue-20-Instructor-end*/

        }
    });
    /*p1 - 196 Rationalization Of Menu Changes - end*/
    //p5 - Issue - 20 - Instructor - start
    $('#ta_field_types').on('change', function () {
        const selFieldTypeId = $(this).val();

        if (!selFieldTypeId) { // None
            $('#fieldSettings').hide();
            $('#positionSettings').hide();
            $('#FinancialReimbursementSettings').hide();
            $('#irNotes').hide();

            gridTAFields.clear();

            gridTAFields.hide();

        } else { // Instructor name and Position
            $('#fieldSettings').show();


            gridTAFields.clear();
            gridTAFields.show();
            gridTAFields.reload();
        }


    });
    //p5 - Issue - 20 - Instructor - end



    /*Client Site SOP dileep 17092024*/
    let gridSchedules;



    gridSchedules = $('#staff_document_siteSOP').grid({
        //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=4',
        dataSource: {
            url: '/Admin/Settings?handler=StaffDocsUsingType&&type=4',
            data: function () {
                return { query: $('#searchBoxTempAndForms').val() }; // Include query dynamically
            }
        },
        uiLibrary: 'bootstrap4',
        iconsLibrary: 'fontawesome',
        primaryKey: 'id',
        columns: [

            { field: 'clientTypeName', title: 'Client Type', width: 160 },
            { field: 'clientSiteName', title: 'Client Site', width: 160 },
            { field: 'fileName', title: 'File Name', width: 300 },
            { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 150 },
            { width: 55, field: 'sop', title: 'SOP' },
            { width: 140, renderer: schButtonRenderer },
        ],
        initialized: function (e) {
            $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
        }
    });

    // Search functionality
    $('#searchBoxTempAndForms').on('input', function () {
        const query = $(this).val(); // Get the search query
        gridStaffDocsTypeTemplatesAndForms.reload({ query: query }); // Pass the updated query to reload
        gridSchedules.reload({ query: query });
        gridStaffDocsTypeCompanySop.reload({ query: query });
        gridStaffDocsTypeTraining.reload({ query: query });
        gridSchedulesAlarm.reload({ query: query });

    });
    //if ($('#sel_client_type').val() != null && $('#sel_client_type').val() != '' && $('#sel_client_type').val() != undefined) {

    //    //keyVehicleLog.search(item).draw();
    //    //gridSchedules.clear();
    //    //gridSchedules.search($('#sel_client_type').val()).draw();
    //    var value = $('#sel_client_type option:selected').text();
    //    alert(value);
    //    $("#staff_document_siteSOP tbody tr").filter(function () {
    //        $(this).toggle(value)
    //    });
    //gridSchedules.reload({ clientTypeName: $('#sel_client_type').val(), searchTerm: $('#search_kw_client_site').val() });
    //}
    function schButtonRenderer(value, record) {
        let buttonHtml = '';
        buttonHtml = '<a href="/StaffDocs/' + record.fileName + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-download"></i></a>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary m-1 d-block" data-toggle="modal" data-target="#schedule-modal" data-sch-id="' + record.id + '" ';
        buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil"></i></button>';
        buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-danger m-1 del-schedule d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash" aria-hidden="true"></i></button>';
        return buttonHtml;
    }



    $('#clientTypeNamesiteSOP').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSitessiteSOP').html('');
            $('#clientSitessiteSOP').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesSOPClientSite&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSitessiteSOP').html('');
            $('#clientSitessiteSOP').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#clientSitessiteSOP').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        });
    });

    $('#clientTypeNamesiteSOPAlarm').on('change', function () {
        const option = $(this).val();
        if (option === '') {
            $('#clientSitessiteSOPAlarm').html('');
            $('#clientSitessiteSOPAlarm').append('<option value="">Select</option>');
        }

        $.ajax({
            url: '/Admin/Settings?handler=ClientSitesSOPClientSite&type=' + encodeURIComponent(option),
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#clientSitessiteSOPAlarm').html('');
            $('#clientSitessiteSOPAlarm').append('<option value="">Select</option>');
            data.map(function (site) {
                $('#clientSitessiteSOPAlarm').append('<option value="' + site.value + '">' + site.text + '</option>');
            });
        });
    });


    $('#removeSelectedSites').on('click', function () {
        $('#selectedSites option:selected').remove();
        updateSelectedSitesCount();
    });

    function updateSelectedSitesCount() {
        $('#selectedSitesCount').text($('#selectedSites option').length);
    }

    $('#schedule-modal').on('shown.bs.modal', function (event) {
        clearScheduleModal();
        const button = $(event.relatedTarget);
        const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
        if (isEdit) {
            schId = button.data('sch-id');
            SOPClientSiteModalOnEdit(schId);
        }


    });

    function clearScheduleModal() {
        $('#dynamicLabel').html('');
        $('#filename').val('');
        $('#add_staff_document_file_SlientSite_sop').val('');
        $('#scheduleId').val('0');
        $('#clientTypeNamesiteSOP').val('');
        $('#clientSitessiteSOP').html('<option value="">Select</option>');
        $('#clientTypeNamesiteSOP option:eq(0)').attr('selected', true);
        $('#GroupName').val('');
        $('#sch-modal-validation').hide();
        $('#SOP').prop('selectedIndex', 0);
    }


    function SOPClientSiteModalOnEdit(scheduleId) {
        $('#loader').show();
        $.ajax({
            url: '/Admin/Settings?handler=SOPClientSitebyId&id=' + scheduleId,
            type: 'GET',
            dataType: 'json',
        }).done(function (data) {
            $('#scheduleId').val(data.id);
            $('#filename').val(data.fileName);
            $('#dynamicLabel').html('<a href="/StaffDocs/' + data.fileName + '" class="btn btn-outline-primary" target="_blank"><i class="fa fa-download"></i> Download</a>');

            $('#clientSitessiteSOP').html(''); // Clear the existing options
            $('#clientSitessiteSOP').append('<option value="">Select</option>'); // Add a default 'Select' option

            // Populate the dropdown with new options
            $.each(data.clientSites, function (index, item) {
                $('#clientSitessiteSOP').append('<option value="' + item.value + '">' + item.text + '</option>');
            });
            $('#clientTypeNamesiteSOP').val(data.clientTypeName);
            // Set the value of the dropdown and trigger the change event
            $('#clientSitessiteSOP').val(data.clientSite);

            // Set the value for another input field
            $('#SOP').val(data.sop);

        }).always(function () {
            $('#loader').hide();
        });
    }

    $('#staff_document_siteSOP').on('click', '.del-schedule', function () {
        const idToDelete = $(this).attr('data-sch-id');
        if (confirm('Are you sure want to delete this linked Duress?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteStaffDoc',
                type: 'POST',
                data: { id: idToDelete },
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function () {
                gridSchedules.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
            });
        }

    });

    $('#btnSaveSiteSOP').on('click', function () {
        $('#loadinDiv').show();
        var fileName = $('#filename').val();
        var sop = $('#SOP').val();
        var site = $('#clientSitessiteSOP').val();
        var scheduleId = $('#scheduleId').val();
        if (site == '') {

            $('#add_staff_document_file_SlientSite_sop').val('');
            $('#msg-modal .modal-body p').html('Please select Site');
            $('#msg-modal').modal();
            return false;

        }
        var fileInput = $('#add_staff_document_file_SlientSite_sop')[0]; // Access the raw DOM element
        const fileForm = new FormData();
        // Check if a file has been selected
        if (fileInput.files.length > 0) {
            const file = fileInput.files[0]; // Get the first file object
            const fileExtn = file.name.split('.').pop().toLowerCase(); // Get file extension
            fileName = file.name;
            // Validate file extension
            if (!fileExtn || ['pdf', 'docx', 'xlsx'].indexOf(fileExtn) < 0) {
                showModal('Unsupported file type. Please upload a .pdf, .docx, or .xlsx file');
                return false;
            }
            // Prepare the form data

            fileForm.append('file', file);



        }
        else {

            if (scheduleId == 0) {
                showModal('Please select the file to upload');
                return false;
            }
            fileForm.append('file', '');
        }

        fileForm.append('type', 4);
        fileForm.append('sop', sop);
        fileForm.append('site', site);
        fileForm.append('doc-id', scheduleId);
        fileForm.append('fileName', fileName);

        //if (edit)
        //    fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=UploadStaffDocUsingTypeFour',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {
                gridSchedules.reload();
                gridStaffDocsTypeCompanySop.reload();
                gridStaffDocsTypeTraining.reload();
                gridStaffDocsTypeTemplatesAndForms.reload();
                $('#schedule-modal').modal('hide');
                showStatusNotification(data.success, data.message);

            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#loadinDiv').hide();
        });



    });

    /**Client Site SOP dileep 17092024 end*/



    $('#btnSaveContractorDomain').on('click', function () {
        $('#loadinDiv').show();


        var domainId = $('#domainId').val();
        var fileName = $('#Domainfilename').val();
        var domainName = $('#domainName').val();
        var siteTypeId = $('#siteTypeId').val();
        var checkDomainStatus = $('#checkDomainStatus').is(':checked');

        if (domainName == '') {

            $('#add_ContractorLogo').val('');
            $('#msg-modal .modal-body p').html('Please enter Domain');
            $('#msg-modal').modal();
            return false;

        }
        var fileInput = $('#add_ContractorLogo')[0]; // Access the raw DOM element
        const fileForm = new FormData();
        // Check if a file has been selected
        if (fileInput.files.length > 0) {
            const file = fileInput.files[0]; // Get the first file object
            const fileExtn = file.name.split('.').pop().toLowerCase(); // Get file extension
            fileName = file.name;
            // Validate file extension
            if (!fileExtn || ['jpg', 'png', 'jpeg', 'gif'].indexOf(fileExtn) < 0) {
                showModal('Unsupported file type. Please upload a .jpg, .jpeg, .png, or .gif file');
                return false;
            }
            // Prepare the form data

            fileForm.append('file', file);



        }
        else {
            if (domainId == 0) {
                showModal('Please select the image for Logo');
                return false;

            }
            fileForm.append('file', '');
        }


        var subdomainPattern = /^[a-zA-Z0-9]([a-zA-Z0-9-]{1,61}[a-zA-Z0-9])?$/;

        if (!subdomainPattern.test(domainName)) {
            showModal('Please enter a valid domain name. The name should be less than 50 characters long without spaces or special characters.');
            return false;
        }

        fileForm.append('domainId', domainId);
        fileForm.append('domainName', domainName);
        fileForm.append('siteTypeId', siteTypeId);
        fileForm.append('checkDomainStatus', checkDomainStatus);
        fileForm.append('fileName', fileName);

        //if (edit)
        //    fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

        $.ajax({
            url: '/Admin/Settings?handler=ClientSiteTypeDomainSettings',
            type: 'POST',
            data: fileForm,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function (data) {
            if (data.success) {

                reloadgridType();
                gridType.reload();
                $('#siteTypeDomainDeatils').modal('hide');
                showStatusNotification(data.success, data.message);

            }
            else {

                showModal(data.message);
            }
        }).fail(function () {
            showStatusNotification(false, 'Something went wrong');
        }).always(function () {
            $('#loadinDiv').hide();
        });



    });

});

//Client SOP(Alarm)
const showStatusNotification = function (success, message) {
    if (success) {
        $('.toast .toast-header strong').removeClass('text-danger').addClass('text-success').html('Success');
    } else {
        $('.toast .toast-header strong').removeClass('text-success').addClass('text-danger').html('Error');
    }
    $('.toast .toast-body').html(message);
    $('.toast').toast('show');
}
$('#btnSaveSiteSOPAlarm').on('click', function () {
    $('#loadinDiv').show();
    var fileName = $('#filenameAlarm').val();
    var sop = $('#SOPAlarm').val();
    var site = $('#clientSitessiteSOPAlarm').val();
    var scheduleId = $('#scheduleId1').val();
    const showModal = function (message) {

        $('#msg-modal .modal-body p').html(message);
        $('#msg-modal').modal();
    }
    if (site == '') {

        $('#add_staff_document_file_SlientSite_sopAlarm').val('');
        $('#msg-modal .modal-body p').html('Please select Site');
        $('#msg-modal').modal();
        return false;

    }
    var fileInput = $('#add_staff_document_file_SlientSite_sopAlarm')[0]; // Access the raw DOM element
    const fileForm = new FormData();
    // Check if a file has been selected
    if (fileInput.files.length > 0) {
        const file = fileInput.files[0]; // Get the first file object
        const fileExtn = file.name.split('.').pop().toLowerCase(); // Get file extension
        fileName = file.name;
        // Validate file extension
        if (!fileExtn || ['pdf', 'docx', 'xlsx'].indexOf(fileExtn) < 0) {
            showModal('Unsupported file type. Please upload a .pdf, .docx, or .xlsx file');
            return false;
        }
        // Prepare the form data

        fileForm.append('file', file);



    }
    else {

        if (scheduleId == 0) {
            showModal('Please select the file to upload');
            return false;
        }
        fileForm.append('file', '');
    }

    fileForm.append('type', 6);
    fileForm.append('sop', sop);
    fileForm.append('site', site);
    fileForm.append('doc-id', scheduleId);
    fileForm.append('fileName', fileName);

    //if (edit)
    //    fileForm.append('doc-id', uploadCtrl.attr('data-doc-id'));

    $.ajax({
        url: '/Admin/Settings?handler=UploadStaffDocUsingTypeSix',
        type: 'POST',
        data: fileForm,
        processData: false,
        contentType: false,
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
    }).done(function (data) {
        if (data.success) {

            gridSchedulesAlarm.reload();
            $('#schedule-modalAlarm').modal('hide');
            showStatusNotification(data.success, data.message);

        }
    }).fail(function () {
        showStatusNotification(false, 'Something went wrong');
    }).always(function () {
        $('#loadinDiv').hide();
    });



});
let gridSchedulesAlarm;



gridSchedulesAlarm = $('#staff_document_siteSOPAlarm').grid({
    //dataSource: '/Admin/Settings?handler=StaffDocsUsingType&&type=4',
    dataSource: {
        url: '/Admin/Settings?handler=StaffDocsUsingType&&type=6',
        data: function () {
            return { query: $('#searchBoxTempAndForms').val() }; // Include query dynamically
        }
    },
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [

        { field: 'clientTypeName', title: 'Client Type', width: 160 },
        { field: 'clientSiteName', title: 'Client Site', width: 160 },
        { field: 'fileName', title: 'File Name', width: 300 },
        { field: 'formattedLastUpdated', title: 'Date & Time Updated', width: 150 },
        { width: 55, field: 'sop', title: 'SOP' },
        { width: 140, renderer: schButtonRenderer1 },
    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});
function schButtonRenderer1(value, record) {
    let buttonHtml = '';
    buttonHtml = '<a href="' + record.filePath + record.fileName + '" class="btn btn-outline-primary m-1" target="_blank"><i class="fa fa-download"></i></a>';
    buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-primary m-1 d-block" data-toggle="modal" data-target="#schedule-modalAlarm" data-sch-id="' + record.id + '" ';
    buttonHtml += 'data-action="editScheduleAlarm"><i class="fa fa-pencil"></i></button>';
    buttonHtml += '<button style="display:inline-block!important;" class="btn btn-outline-danger m-1 del-scheduleAlarm d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash" aria-hidden="true"></i></button>';
    return buttonHtml;
}
$('#schedule-modalAlarm').on('shown.bs.modal', function (event) {
    clearScheduleAlarmModal();
    const button = $(event.relatedTarget);
    const isEdit = button.data('action') !== undefined && button.data('action') === 'editScheduleAlarm';
    if (isEdit) {
        schId = button.data('sch-id');
        SOPClientSiteModalOnEditAlarm(schId);
    }


});
$('#staff_document_siteSOPAlarm').on('click', '.del-scheduleAlarm', function () {
    const idToDelete = $(this).attr('data-sch-id');
    if (confirm('Are you sure want to delete this linked Duress?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteStaffDoc',
            type: 'POST',
            data: { id: idToDelete },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridSchedulesAlarm.reload({ type: $('#sel_schedule').val(), searchTerm: $('#search_kw_client_site').val() });
        });
    }

});
function clearScheduleAlarmModal() {
    $('#dynamicLabelAlarm').html('');
    $('#filenameAlarm').val('');
    $('#add_staff_document_file_SlientSite_sopAlarm').val('');
    $('#scheduleId1').val('0');
    $('#clientTypeNamesiteSOPAlarm').val('');
    $('#clientSitessiteSOPAlarm').html('<option value="">Select</option>');
    $('#clientTypeNamesiteSOPAlarm option:eq(0)').attr('selected', true);
    $('#GroupName').val('');
    $('#sch-modal-validation').hide();
    $('#SOPAlarm').prop('selectedIndex', 0);
}


function SOPClientSiteModalOnEditAlarm(scheduleId) {
    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=SOPClientSitebyId&id=' + scheduleId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#scheduleId1').val(data.id);
        $('#filenameAlarm').val(data.fileName);
        $('#dynamicLabelAlarm').html('<a href="/StaffDocs/' + data.fileName + '" class="btn btn-outline-primary" target="_blank"><i class="fa fa-download"></i> Download</a>');

        $('#clientSitessiteSOPAlarm').html(''); // Clear the existing options
        $('#clientSitessiteSOPAlarm').append('<option value="">Select</option>'); // Add a default 'Select' option

        // Populate the dropdown with new options
        $.each(data.clientSites, function (index, item) {
            $('#clientSitessiteSOPAlarm').append('<option value="' + item.value + '">' + item.text + '</option>');
        });
        $('#clientTypeNamesiteSOPAlarm').val(data.clientTypeName);
        // Set the value of the dropdown and trigger the change event
        $('#clientSitessiteSOPAlarm').val(data.clientSite);

        // Set the value for another input field
        $('#SOPAlarm').val(data.sop);

    }).always(function () {
        $('#loader').hide();
    });
}

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
/*p1-191 hr files task 3-start*/
let gridHrSettings;
//if ($('#tbl_hr_settings').length === 1) {
//    gridHrSettings = new DataTable('#tbl_hr_settings', {
//        paging: false,
//        searching: true,
//        ordering: false,
//        info: false,
//        scrollX: true,
//        ajax: {
//            url: '/Admin/Settings?handler=ClientSites',
//            data: function (d) {
//                d.logbookId = $('#KeyVehicleLog_ClientSiteLogBookId').val();
//                d.kvlStatusFilter = $('#kvl_status_filter').val();
//            },
//            dataSrc: ''
//        },
//        columns: [
//            { data: 'detail.id', visible: false },

//            { data: 'detail.initialCallTime', width: '100%', title:'HR Group' },
//            { data: 'detail.entryTime', width: '100%', type: 'dropdown', title: 'Reference No' },
//            { data: 'detail.initialCallTime', width: '100%', title: 'Description' },
//            {
//                targets: -1,
//                data: 'detail.id',
//                width: '20%',
//                defaultContent: '',

//                render: function (value, type, data) {
//                    return '<button id="btnEditHRSettings" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
//                        '<button id="btnDeleteHRSettings" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash"></i></button>' +
//                        '</div>'


//                        ;
//                    // if (value === null) return 'N/A';
//                    // return value != 0 ? '<i class="fa fa-check-circle text-success rc-client-status"></i>' + ' [' + '<a href="#guardLogBookInfoModal" id="btnLogBookDetailsByGuard">' + value + '</a>' + '] <input type="hidden" id="ClientSiteId" value="' + value + '"><input type="hidden" id="GuardId" value="' + value + '">' : '<i class="fa fa-times-circle text-danger rc-client-status"></i><input type="hidden" id="ClientSiteId" text="' + value + '"><input type="hidden" id="GuardId" text="' + value + '"> ';
//                }
//                //render: function (data, type, row) {
//                //    return '<input type="checkbox" id=' + data.detail.keyNo+'/>';
//                //}
//                //defaultContent: '<button id="btnEditVkl" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
//                //   '<button id="btnPrintVkl" class="btn btn-outline-primary mr-1 "><i class="fa fa-print"></i></button>' +
//                //    '<button id="btnDeleteVkl" class="btn btn-outline-danger mr-2 mt-1"><i class="fa fa-trash mr-2"></i>Delete</button>',
//            }],

//    });

//    var dataTable = $('#vehicle_key_daily_log').DataTable();
//    var ids = [];
//    dataTable.rows().data().each(function (index, rowData) {

//        ids.push(index.detail.id);
//    });
//}

gridHrSettings = $('#tbl_hr_settings').grid({
    dataSource: '/Admin/GuardSettings?handler=HRSettings',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    detailTemplate: '<div class="bg-light"><b>Sites:</b><br>{clientSites}</div>',
    showHiddenColumnsAsDetails: false,
    primaryKey: 'id',
    icons: {
        expandRow: '<i class="fa fa-arrow-circle-o-right fa-2x text-success" aria-hidden="true"></i>',
        collapseRow: '<i class="fa fa-arrow-circle-o-down fa-2x text-success" aria-hidden="true"></i>'
    },
    columns: [
        { field: 'id', hidden: true },
        { field: 'groupName', width: '10%' }, // Show the HR Group column
        { field: 'referenceNo', width: '10%' },
        { field: 'description', width: '30%' },
        { field: 'states' },
        { field: 'clientSitesSummary' },
        { width: '5%', renderer: hrgroupLockButtonRenderer },
        { width: '5%', renderer: hrgroupEditBanButtonRenderer },

        { width: '10%', renderer: hrgroupButtonRenderer },
    ],
    dataBound: function (e, records, totalRecords) {
        var tbody = $(e.target).find('tbody');
        var rows = tbody.find('tr');

        var lastGroupValue = null;

        rows.each(function (index, row) {
            var expandbutton

            var currentGroupValue = $(row).find('td:eq(2)').text();
            if (currentGroupValue !== lastGroupValue) {
                lastGroupValue = currentGroupValue;

                var headerRow = $('<tr>').addClass('group-header').append($('<th>').attr('colspan', 9).text(currentGroupValue));
                headerRow.css('background-color', '#CCCCCC');
                $(row).before(headerRow);
            }
        });
    },
    initialized: function (e) {
        // Optionally, you can modify the appearance or behavior after the grid is initialized
        $('#tbl_hr_settings thead tr th:last')
            .prev()
            .prev()// Select the column before the last
            .addClass('text-center')
            .html('<i class="fa fa-lock" aria-hidden="true"></i>');
        $('#tbl_hr_settings thead tr th:last')
            .prev() 
           
            .addClass('text-center')
            .html('<i class="fa fa-ban" aria-hidden="true"></i>');
        $('#tbl_hr_settings thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});
let gridHrSettingswithCourseLibrary;
gridHrSettingswithCourseLibrary = $('#tbl_hr_settings_with_CourseLibrary').grid({
    dataSource: '/Admin/GuardSettings?handler=HRSettingsWithCourseLibrary',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    detailTemplate: '<div class="bg-light"><b>Sites:</b><br>{clientSites}</div>',
    showHiddenColumnsAsDetails: false,
    primaryKey: 'id',
    icons: {
        expandRow: '<i class="fa fa-arrow-circle-o-right fa-2x text-success" aria-hidden="true"></i>',
        collapseRow: '<i class="fa fa-arrow-circle-o-down fa-2x text-success" aria-hidden="true"></i>'
    },
    columns: [
        { field: 'id', hidden: true },
        { field: 'groupName', width: '10%' }, // Show the HR Group column
        { field: 'referenceNo', width: '10%' },
        { field: 'description', width: '30%' },
        { field: 'states' },
        { field: 'clientSitesSummary' },
        { width: '5%', renderer: courseStatusColorRenderer },
        { width: '5%', renderer: hrgroupLockButtonRenderer },
        { width: '5%', renderer: hrgroupEditBanButtonRenderer },

        { width: '10%', renderer: hrgroupButtonRenderer },
    ],
    dataBound: function (e, records, totalRecords) {
        var tbody = $(e.target).find('tbody');
        var rows = tbody.find('tr');

        var lastGroupValue = null;

        rows.each(function (index, row) {
            var expandbutton

            var currentGroupValue = $(row).find('td:eq(2)').text();
            if (currentGroupValue !== lastGroupValue) {
                lastGroupValue = currentGroupValue;

                var headerRow = $('<tr>').addClass('group-header').append($('<th>').attr('colspan', 9).text(currentGroupValue));
                headerRow.css('background-color', '#CCCCCC');
                $(row).before(headerRow);
            }
        });
    },
    initialized: function (e) {
        // Optionally, you can modify the appearance or behavior after the grid is initialized
        $('#tbl_hr_settings_with_CourseLibrary thead tr th:last')
            .prev()
            .prev()// Select the column before the last
            .addClass('text-center')
            .html('<i class="fa fa-lock" aria-hidden="true"></i>');
        $('#tbl_hr_settings_with_CourseLibrary thead tr th:last')
            .prev()

            .addClass('text-center')
            .html('<i class="fa fa-ban" aria-hidden="true"></i>');
        $('#tbl_hr_settings_with_CourseLibrary thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }
});
function courseStatusColorRenderer(value, record) {
    var cellvalue;
    if (record.courseColour == 'Yellow') {
        cellvalue = '<div class="text-center">' +
            '<i class="fa fa-circle text-warning" style="padding-top:10px;"></i>'
            + '</div>';
    }
    else {
        cellvalue = '<div class="text-center">' +
            '<i class="fa fa-circle text-success" style="padding-top:10px;"></i>' + '</div>';
    }
    
   
    return cellvalue;
}
function hrgroupLockButtonRenderer(value, record) {
    if (record.hrlock) {
        // return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnLock"    data-doc-id="' + record.id + '" data-lock-status=1 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></a>' +
            '</div>'
    }
    else {

        //return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnLock"   data-doc-id="' + record.id + '" data-lock-status=0 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></a>' +
            '</div>'
    }
}
function hrgroupLockButtonRenderer(value, record) {
    if (record.hrlock) {
        // return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnLock"    data-doc-id="' + record.id + '" data-lock-status=1 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></a>' +
            '</div>'
    }
    else {

        //return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnLock"   data-doc-id="' + record.id + '" data-lock-status=0 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></a>' +
            '</div>'
    }
}

function hrgroupEditBanButtonRenderer(value, record) {
    if (record.hrbanedit) {
        // return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnBan"    data-doc-id="' + record.id + '" data-ban-status=1 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkenabled.png"  style="padding-top:10px;" /></a>' +
            '</div>'
    }
    else {

        //return '<button class="btn btn-outline-primary" data-toggle="modal" data-target="#user-client-access-modal-lock" data-id="{id}"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></button>'
        return '<div class="text-center">' +
            '<a id="btnBan"   data-doc-id="' + record.id + '" data-ban-status=0 data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '"><img src="../images/icons/chkdesabled.png" style="padding-top:10px;" /></a>' +
            '</div>'
    }
}
function hrgroupButtonRenderer(value, record) {
    return '<div class="text-center">' +
        '<button id="btnEditHrGroup" data-doc-id="' + record.id + '" data-doc-hrgroupid="' + record.hrGroupId + '" data-doc-refnonumberid="' + record.referenceNoNumberId + '" data-doc-refalphnumberid="' + record.referenceNoAlphabetId + '" data-doc-description="' + record.description + '" class="btn btn-outline-primary mr-2"><i class="fa fa-pencil"></i></button>' +
        '<button id="btnDeleteHrGroup" data-doc-id="' + record.id + '" class="btn btn-outline-danger"><i class="fa fa-trash"></i></button>' +

        '</div>'
}
let isHrSettingsAdding = false
let isLoteAdding = false
//$('#user_settings').on('click', '.deleteuser', function () {
//    if (confirm('Are you sure want to delete this user?')) {
//        toggleUserStatus($(this).attr('data-user-id'), true);
//    }
//});
let ucaTreeLock;
$('#tbl_hr_settings tbody').on('click', '#btnLock', function () {
    $('#lockHRRcord').prop('checked', false);
    const userId = $(this).attr('data-doc-id');
    const lockStatus = $(this).attr('data-lock-status');
    $('#user-access-for-idlock').val($(this).attr('data-doc-id'));
    if (lockStatus === '1') {
        $('#lockHRRcord').prop('checked', true);
    }
    else {
        $('#lockHRRcord').prop('checked', false);
    }

    if (ucaTreeLock === undefined) {



        ucaTreeLock = $('#ucaTreeViewlock').tree({
            uiLibrary: 'bootstrap4',
            checkboxes: true,
            primaryKey: 'id',
            dataSource: '/Admin/Settings?handler=HrSettingsLockedClientSites',
            autoLoad: false,
            textField: 'name',
            childrenField: 'clientSites',
            checkedField: 'checked'
        });
    }
    else {

        ucaTreeLock.destroy(); // Destroys and removes the current tree

        // Clear the container's inner HTML to remove any tree remnants
        $('#ucaTreeViewlock').empty();

        ucaTreeLock = $('#ucaTreeViewlock').tree({
            uiLibrary: 'bootstrap4',
            checkboxes: true,
            primaryKey: 'id',
            dataSource: '/Admin/Settings?handler=HrSettingsLockedClientSites',
            autoLoad: false,
            textField: 'name',
            childrenField: 'clientSites',
            checkedField: 'checked'
        });
    }

    ucaTreeLock.uncheckAll();
    ucaTreeLock.reload({ hrSttingsId: userId });
    $('#loader').show();
    $('#user-client-access-modal-lock').modal('show');
});

$('#tbl_hr_settings tbody').on('click', '#btnBan', function () {
    const userId = $(this).attr('data-doc-id');
    let enableStatus = 0;
    let Status = $(this).attr('data-ban-status');
    if (Status == 1) {
        enableStatus = 0
    }
    else {
        enableStatus = 1;
    }

    
    $.ajax({
        url: '/Admin/Settings?handler=HrSettingsBanEdit',
        data: {
            hrSttingsId: userId,
            enableStatus: enableStatus
        },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        gridHrSettings.reload();
        gridHrSettingswithCourseLibrary.reload();
        //showStatusNotification(true, 'Saved successfully');
    }).fail(function () {
        console.log('error');
    });

   
});


$('#tbl_hr_settings tbody').on('click', '#btnEditHrGroup', function () {


    $('#loader').show();
    $('#hrSettingsModal').modal('show');
    $('#HrSettings_Id').val($(this).attr('data-doc-id'));
    $('#list_hrGroups').val($(this).attr('data-doc-hrgroupid'));
    $('#list_ReferenceNoNumber').val($(this).attr('data-doc-refnonumberid'));
    $('#list_ReferenceNoAlphabet').val($(this).attr('data-doc-refalphnumberid'));
    $('#txtHrSettingsDescription').val($(this).attr('data-doc-description'));

    $.ajax({
        url: '/Admin/GuardSettings?handler=HrSettingById&id=' + $(this).attr('data-doc-id'),
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        clearCriticalModalHrDoc();
        var selectedValues = [];
        $.each(data.hrSettingsClientStates, function (index, item2) {
            selectedValues.push(item2.state);
        });

        $("#HrState").multiselect();
        $("#HrState").val(selectedValues);
        $("#HrState").multiselect("refresh");
        $("#clientTypeNameDocHrDoc").multiselect("refresh");

        $.each(data.hrSettingsClientSites, function (index, item) {
            $('#selectedSitesDocHrDoc').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            updateSelectedSitesCountHrDoc();

        });
        $("#clientSitesDocHrDoc").multiselect("refresh");
    }).always(function () {
        $('#loader').hide();
    });






});
$('#tbl_hr_settings tbody').on('click', '#btnDeleteHrGroup', function () {
    // var data = keyVehicleLog.row($(this).parents('tr')).data();
    if (confirm('Are you sure want to delete this  entry?')) {
        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=DeleteHRSettings',
            data: { 'id': $(this).attr('data-doc-id') },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function () {
            gridHrSettings.reload();
            gridHrSettingswithCourseLibrary.reload();
        }).always(function () {
            $('#loader').hide();
        });
    }
});
$('#tbl_hr_settings_with_CourseLibrary tbody').on('click', '#btnLock', function () {
    $('#lockHRRcord').prop('checked', false);
    const userId = $(this).attr('data-doc-id');
    const lockStatus = $(this).attr('data-lock-status');
    $('#user-access-for-idlock').val($(this).attr('data-doc-id'));
    if (lockStatus === '1') {
        $('#lockHRRcord').prop('checked', true);
    }
    else {
        $('#lockHRRcord').prop('checked', false);
    }

    if (ucaTreeLock === undefined) {



        ucaTreeLock = $('#ucaTreeViewlock').tree({
            uiLibrary: 'bootstrap4',
            checkboxes: true,
            primaryKey: 'id',
            dataSource: '/Admin/Settings?handler=HrSettingsLockedClientSites',
            autoLoad: false,
            textField: 'name',
            childrenField: 'clientSites',
            checkedField: 'checked'
        });
    }
    else {

        ucaTreeLock.destroy(); // Destroys and removes the current tree

        // Clear the container's inner HTML to remove any tree remnants
        $('#ucaTreeViewlock').empty();

        ucaTreeLock = $('#ucaTreeViewlock').tree({
            uiLibrary: 'bootstrap4',
            checkboxes: true,
            primaryKey: 'id',
            dataSource: '/Admin/Settings?handler=HrSettingsLockedClientSites',
            autoLoad: false,
            textField: 'name',
            childrenField: 'clientSites',
            checkedField: 'checked'
        });
    }

    ucaTreeLock.uncheckAll();
    ucaTreeLock.reload({ hrSttingsId: userId });
    $('#loader').show();
    $('#user-client-access-modal-lock').modal('show');
});
$('#tbl_hr_settings_with_CourseLibrary tbody').on('click', '#btnBan', function () {
    const userId = $(this).attr('data-doc-id');
    let enableStatus = 0;
    let Status = $(this).attr('data-ban-status');
    if (Status == 1) {
        enableStatus = 0
    }
    else {
        enableStatus = 1;
    }


    $.ajax({
        url: '/Admin/Settings?handler=HrSettingsBanEdit',
        data: {
            hrSttingsId: userId,
            enableStatus: enableStatus
        },
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function () {
        gridHrSettingswithCourseLibrary.reload();
        //showStatusNotification(true, 'Saved successfully');
    }).fail(function () {
        console.log('error');
    });


});
$('#tbl_hr_settings_with_CourseLibrary tbody').on('click', '#btnEditHrGroup', function () {


    $('#loader').show();
    $('#hrSettingsModal').modal('show');
    $('#HrSettings_Id').val($(this).attr('data-doc-id'));
    $('#list_hrGroups').val($(this).attr('data-doc-hrgroupid'));
    $('#list_ReferenceNoNumber').val($(this).attr('data-doc-refnonumberid'));
    $('#list_ReferenceNoAlphabet').val($(this).attr('data-doc-refalphnumberid'));
    $('#txtHrSettingsDescription').val($(this).attr('data-doc-description'));

    $.ajax({
        url: '/Admin/GuardSettings?handler=HrSettingById&id=' + $(this).attr('data-doc-id'),
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        clearCriticalModalHrDoc();
        var selectedValues = [];
        $.each(data.hrSettingsClientStates, function (index, item2) {
            selectedValues.push(item2.state);
        });

        $("#HrState").multiselect();
        $("#HrState").val(selectedValues);
        $("#HrState").multiselect("refresh");
        $("#clientTypeNameDocHrDoc").multiselect("refresh");

        $.each(data.hrSettingsClientSites, function (index, item) {
            $('#selectedSitesDocHrDoc').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            updateSelectedSitesCountHrDoc();

        });
        $("#clientSitesDocHrDoc").multiselect("refresh");

        //To Show wherether the course file is added-start
        ShowStatusColorForCourse();
        //To Show wherether the course file is added-end
    }).always(function () {
        $('#loader').hide();
    });






});
function ShowStatusColorForCourse() {
    var hrSettingsId=$('#HrSettings_Id').val();
    $.ajax({
        url: '/Admin/Settings?handler=CourseStatusColorById&hrSettingsid=' + hrSettingsId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        if (data.courseLength == true) {
            $('#statusCourseColor').removeClass('text-warning');
            $('#statusCourseColor').addClass('text-success');
        }
        else {
            $('#statusCourseColor').removeClass('text-success');

            $('#statusCourseColor').addClass('text-warning');
        }
        if (data.testQuestionsLength == true) {
            $('#statusTestQuestionsColor').removeClass('text-warning');
            $('#statusTestQuestionsColor').addClass('text-success');
        }
        else {
            $('#statusTestQuestionsColor').removeClass('text-success');

            $('#statusTestQuestionsColor').addClass('text-warning');
        }
        
        if (data.certificateLength == true) {
            $('#statusCertificateColor').removeClass('text-warning');
            $('#statusCertificateColor').addClass('text-success');
        }
        else {
            $('#statusCertificateColor').removeClass('text-success');

            $('#statusCertificateColor').addClass('text-warning');
        }
       
    }).always(function () {
        $('#loader').hide();
    });
}
$('#tbl_hr_settings_with_CourseLibrary tbody').on('click', '#btnDeleteHrGroup', function () {
    // var data = keyVehicleLog.row($(this).parents('tr')).data();
    if (confirm('Are you sure want to delete this entry?')) {
        $.ajax({
            type: 'POST',
            url: '/Admin/GuardSettings?handler=DeleteHRSettings',
            data: { 'id': $(this).attr('data-doc-id') },
            dataType: 'json',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function () {
            gridHrSettings.reload();
            gridHrSettingswithCourseLibrary.reload();
        }).always(function () {
            $('#loader').hide();
        });
    }
});

/*Lock Start*/

$('#btnSaveUserAccesslock').on('click', function () {
    if (ucaTreeLock) {

        let enableStatus = 0;
        const userId = $('#user-access-for-idlock').val();
        if ($('#lockHRRcord').is(':checked')) {
            enableStatus = 1;

        }

        let selectedSites = ucaTreeLock.getCheckedNodes().filter(function (item) {
            return item !== 'undefined';
        });
        $.ajax({
            url: '/Admin/Settings?handler=HrSettingsLockedClientSites',
            data: {
                hrSttingsId: userId,
                selectedSites: selectedSites,
                enableStatus: enableStatus
            },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridHrSettings.reload();
            gridHrSettingswithCourseLibrary.reload();
            //showStatusNotification(true, 'Saved successfully');
        }).fail(function () {
            console.log('error');
        });
    }
});

$('#grantAllUserAccesslock').on('click', function () {
    if (ucaTreeLock !== undefined) {
        ucaTreeLock.checkAll();
    }
});

$('#revokeAllUserAccesslock').on('click', function () {
    if (ucaTreeLock !== undefined && confirm('Are you sure want to revoke all access?')) {
        ucaTreeLock.uncheckAll();
    }
});

$('#expandAllUserAccesslock').on('click', function () {
    if (ucaTreeLock !== undefined) {
        ucaTreeLock.expandAll();
    }
});

$('#collapseAllUserAccesslock').on('click', function () {
    if (ucaTreeLock !== undefined) {
        ucaTreeLock.collapseAll();
    }
});


/*Lock end */


let gridLicenseTypes
gridLicenseTypes = $('#tbl_license_type').grid({
    dataSource: '/Admin/GuardSettings?handler=LicensesTypes',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { field: 'id', hidden: true },
        { field: 'name', title: 'Description', width: '100%', editor: true },


    ],

    initialized: function (e) {
        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    },

});
if (gridLicenseTypes) {
    gridLicenseTypes.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveLicensesTypes',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) gridLicenseTypes.reload();
            else alert(result.message);
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isHrSettingsAdding)
                isHrSettingsAdding = false;
        });
    });

    gridLicenseTypes.on('rowRemoving', function (e, id, record) {
        if (confirm('Are you sure want to delete this field?')) {
            $.ajax({
                url: '/Admin/GuardSettings?handler=DeleteLicensesTypes',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridLicenseTypes.reload();
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isHrSettingsAdding)
                    isHrSettingsAdding = false;
            });
        }
    });
}

let gridLanguage
gridLanguage = $('#tbl_language').grid({
    dataSource: '/Admin/GuardSettings?handler=Languages',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    inlineEditing: { mode: 'command' },
    columns: [
        { field: 'id', hidden: true },
        { field: 'language', title: 'Language', width: '100%', editor: true },


    ],

    initialized: function (e) {
        $(e.target).find('thead tr th:last').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    },

});
if (gridLanguage) {
    gridLanguage.on('rowDataChanged', function (e, id, record) {
        const data = $.extend(true, {}, record);
        $.ajax({
            url: '/Admin/Settings?handler=Savelanguages',
            data: { record: data },
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (result) {
            if (result.success) gridLanguage.reload();
            else alert(result.message);
        }).fail(function () {
            console.log('error');
        }).always(function () {
            if (isLoteAdding)
                isLoteAdding = false;
        });
    });

    gridLanguage.on('rowRemoving', function (e, id, record) {
        if (confirm('Are you sure want to delete this field?')) {
            $.ajax({
                url: '/Admin/Settings?handler=DeleteLanguage',
                data: { id: record },
                type: 'POST',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            }).done(function (result) {
                if (result.success) gridLanguage.reload();
                else alert(result.message);
            }).fail(function () {
                console.log('error');
            }).always(function () {
                if (isLoteAdding)
                    isLoteAdding = false;
            });
        }
    });
}

$('#hr_settings_fields_types').on('change', function () {
    const selHTSettingsFieldTypeId = $('#hr_settings_fields_types').val();
    if ($('#hr_settings_fields_types').val() == 1) {
        gridHrSettings.show();
        gridHrSettings.clear();
        gridHrSettings.reload();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridLanguage.hide();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').show();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        $('#add_lote').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();


        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.hide();
    }

    else if ($('#hr_settings_fields_types').val() == 2) {
        gridHrSettings.hide();
        gridLicenseTypes.show();
        gridLicenseTypes.clear();
        gridLicenseTypes.reload();
        gridCriticalDocument.hide();
        gridLanguage.hide();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').show();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        $('#add_lote').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.hide();
    }
    else if ($('#hr_settings_fields_types').val() == 3) {
        $('#add_criticalDocuments').show();
        $('#add_hr_settings').hide();
        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.show();
        gridCriticalDocument.reload();
        gridLanguage.hide();
        $('#add_hr_settings').hide();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        $('#add_lote').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.hide();
    }
    else if ($('#hr_settings_fields_types').val() == 4) {
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').hide();
        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridLanguage.hide();
        $('#add_hr_settings').hide();
        $('#SettingsDiv').show();
        $('#add_lote').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.hide();
        $.ajax({
            url: '/Admin/Settings?handler=SettingsDetails',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $('#hr_compliance_email').val(data.email)
                $('#DropboxDir').val(data.dropboxDir)
            }
        });

    }
    else if ($('#hr_settings_fields_types').val() == 5) {
        $('#TimesheetDiv').show();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').hide();
        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridCriticalDocument.reload();
        gridLanguage.hide();
        $('#add_lote').hide();
        $('#add_hr_settings').hide();
        $('#SettingsDiv').hide();
        gridClassroomLocation.hide();
        gridHrSettingswithCourseLibrary.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        $.ajax({
            url: '/Admin/Settings?handler=TimesheetDetails',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                $('#week').val(data.week)
                $('#Reportfrequency').val(data.time)
                $('#mailbox').val(data.mailid)
                $('#Defaultdropbox').val(data.dropbox)
            }
        });
    }
    else if ($('#hr_settings_fields_types').val() == 6) {

        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridLanguage.show();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').hide();
        $('#add_lote').show();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.hide();
    }
    else if ($('#hr_settings_fields_types').val() == 7) {

        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridLanguage.hide();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').hide();
        $('#add_lote').hide();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        gridClassroomLocation.show();
        $('#add_location').show();
        $('#ClassroomLocationDiv').show();
        gridHrSettingswithCourseLibrary.hide();
    }
    else if ($('#hr_settings_fields_types').val() == 8) {
        
        gridHrSettings.hide();
        gridLicenseTypes.hide();
        gridCriticalDocument.hide();
        gridLanguage.hide();
        $('#add_criticalDocuments').hide();
        $('#add_hr_settings').show();
        $('#SettingsDiv').hide();
        $('#TimesheetDiv').hide();
        $('#add_lote').hide();
        gridClassroomLocation.hide();
        $('#add_location').hide();
        $('#ClassroomLocationDiv').hide();
        gridHrSettingswithCourseLibrary.show();
        gridHrSettingswithCourseLibrary.clear();
        gridHrSettingswithCourseLibrary.reload();
        
    }
    else {
        gridLicenseTypes.hide();
        gridHrSettings.hide();
        gridLanguage.hide();
        gridClassroomLocation.hide();
        gridHrSettingswithCourseLibrary.hide();
    }
});
if ($('#report_module_types_irtemplate').val() == 1) {
    $('#incident_report_pdf_template').show();
    $('#company_sop').hide();
    $('#training').hide();
    $('#templatesandforms').hide();
    $('#clientSOP').hide();
    $('#ClientSOPAlarm').hide();

}

$('#searchBoxTempAndForms').hide();
$('#btnGroupAddon2').hide();

$('#report_module_types_irtemplate').on('change', function () {
    const reportModuletypeIrtemplateId = $('#report_module_types_irtemplate').val();
    if ($('#report_module_types_irtemplate').val() == 1) {

        $('#incident_report_pdf_template').show();
        $('#company_sop').hide();
        $('#training').hide();
        $('#templatesandforms').hide();
        $('#clientSOP').hide();
        $('#searchBoxTempAndForms').hide();
        $('#btnGroupAddon2').hide();
        $('#ClientSOPAlarm').hide();

    }

    else if ($('#report_module_types_irtemplate').val() == 2) {
        $('#incident_report_pdf_template').hide();
        $('#company_sop').show();
        $('#training').hide();
        $('#templatesandforms').hide();
        $('#clientSOP').hide();
        $('#searchBoxTempAndForms').show();
        $('#btnGroupAddon2').show();
        $('#ClientSOPAlarm').hide();

    }
    else if ($('#report_module_types_irtemplate').val() == 3) {
        $('#incident_report_pdf_template').hide();
        $('#company_sop').hide();
        $('#training').show();
        $('#templatesandforms').hide();
        $('#clientSOP').hide();
        $('#searchBoxTempAndForms').show();
        $('#btnGroupAddon2').show();
        $('#ClientSOPAlarm').hide();
    }
    else if ($('#report_module_types_irtemplate').val() == 4) {
        $('#incident_report_pdf_template').hide();
        $('#company_sop').hide();
        $('#training').hide();
        $('#templatesandforms').show();
        $('#clientSOP').hide();
        $('#searchBoxTempAndForms').show();
        $('#btnGroupAddon2').show();
        $('#ClientSOPAlarm').hide();
    }

    else if ($('#report_module_types_irtemplate').val() == 5) {
        $('#incident_report_pdf_template').hide();
        $('#company_sop').hide();
        $('#training').hide();
        $('#templatesandforms').hide();
        $('#clientSOP').show();
        $('#searchBoxTempAndForms').show();
        $('#btnGroupAddon2').show();
        $('#ClientSOPAlarm').hide();

    }
    else if ($('#report_module_types_irtemplate').val() == 6) {
        $('#incident_report_pdf_template').hide();
        $('#company_sop').hide();
        $('#training').hide();
        $('#templatesandforms').hide();
        $('#clientSOP').hide();
        $('#searchBoxTempAndForms').show();
        $('#btnGroupAddon2').show();
        $('#ClientSOPAlarm').show();

    }
    else {
        $('#searchBoxTempAndForms').hide();
        $('#btnGroupAddon2').hide();
    }
});
$('#clientTypeNameDoc').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#clientSitesDoc').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#add_hr_settings').on('click', function () {
    var selFieldTypeId = $('#hr_settings_fields_types').val();
    if (!selFieldTypeId) {
        alert('Please select a field type to update');
        return;
    }
    if (selFieldTypeId == 1 || selFieldTypeId==8) {
        $('#list_hrGroups').val('');
        $('#list_ReferenceNoNumber').val('');
        $('#list_ReferenceNoAlphabet').val('');
        $('#txtHrSettingsDescription').val('');
        $('#HrSettings_Id').val('');

        $('#clientTypeNameDocHrDoc').val('');
        $('#clientSitesDocHrDoc').val('');
        $('#HrState').val('');

        $("#clientTypeNameDocHrDoc").multiselect("refresh");
        $("#clientSitesDocHrDoc").multiselect("refresh");
        $("#HrState").multiselect("refresh");
        $('#hrSettingsModal').modal('show');

        //$('#statusCourseColor').removeClass('text-warning');
        $('#statusCourseColor').addClass('text-warning');

        //$('#statusTestQuestionsColor').removeClass('text-warning');
        $('#statusTestQuestionsColor').addClass('text-warning');

        //$('#statusCertificateColor').removeClass('text-warning');
        $('#statusCertificateColor').addClass('text-warning');

    }
    if ($('#hr_settings_fields_types').val() == 2) {
        if (isHrSettingsAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isHrSettingsAdding = true;
            gridLicenseTypes.addRow({
                'id': -1,
                'name': '',
            }).edit(-1);
        }
    }

});

//p1-213 critical Document start
$('#add_lote').on('click', function () {
    if ($('#hr_settings_fields_types').val() == 6) {
        if (isLoteAdding) {
            alert('Unsaved changes in the grid. Refresh the page');
        } else {
            isLoteAdding = true;
            gridLanguage.addRow({
                'id': -1,
                'language': '',
            }).edit(-1);
        }
    }
});
$('#add_criticalDocuments').on('click', function () {
    $('#clientSitesDoc').html('');
    $('#Critical-modal').modal('show');
    clearCriticalModal();
});
//$('#clientTypeNameDoc').on('change', function () {
//    const option = $(this).find('option:selected').text();;
//    if (option === '') {
//        $('#clientSitesDoc').html('');
//        $('#clientSitesDoc').append('<option value="">Select</option>');
//    }

//    $.ajax({
//        url: '/admin/settings?handler=ClientSitesDoc&type=' + encodeURIComponent(option),
//        type: 'GET',
//        dataType: 'json',
//    }).done(function (data) {
//        $('#clientSitesDoc').html('');
//        $('#clientSitesDoc').append('<option value="">Select</option>');
//        data.map(function (site) {
//            $('#clientSitesDoc').append('<option value="' + site.value + '">' + site.text + '</option>');
//        });
//    });
//});

$('#clientTypeNameDoc').on('change', function () {
    let clientTypeIds = $(this).val().join(';')
    const clientTypeId = clientTypeIds;
    //$('#clientSitesDoc').multiselect("refresh");
    $('#clientSitesDoc').html('');
    const clientSiteControl = $('#clientSitesDoc');
    var selectedOption = $(this).find("option:selected");
    var selectedText = selectedOption.text();

    $.ajax({
        url: '/admin/settings?handler=ClientSitesNew',
        type: 'GET',
        data: {
            typeId: clientTypeId

        },
        dataType: 'json',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {

        data.map(function (site) {
            clientSiteControl.append('<option value="' + site.id + '">' + site.name + '</option>');
        });
        clientSiteControl.multiselect('rebuild');

    });

});

$('#clientSitesDoc').on('change', function () {
    const selectedValues = $(this).val().join(';').split(';');
    selectedValues.forEach(function (value) {
        if (value !== '') {
            const existing = $('#selectedSitesDoc option[value="' + value + '"]');
            if (existing.length === 0) {
                const text = $('#clientSitesDoc option[value="' + value + '"]').text();
                $('#selectedSitesDoc').append('<option value="' + value + '">' + text + '</option>');
            }
        }
    });
    updateSelectedSitesCount();
});
function updateSelectedSitesCount() {
    $('#selectedSitesCountDoc').text($('#selectedSitesDoc option').length);
    $('#selectedDescCountDoc').text($('#selectedDescDoc option').length);
}
$('#HRGroupDoc').on('change', function () {
    const option = $(this).val();
    if (option === '') {
        $('#DescriptionDoc').html('');
        $('#DescriptionDoc').append('<option value="">Select</option>');
    }

    $.ajax({
        url: '/admin/settings?handler=DescriptionList&HRGroupId=' + encodeURIComponent(option),
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#DescriptionDoc').html('');
        $('#DescriptionDoc').append('<option value="">Select</option>');
        data.map(function (site) {
            $('#DescriptionDoc').append('<option value="' + site.value + '">' + site.text + '</option>');
        });
    });
});
$('#DescriptionDoc').on('change', function () {
    var Clientsite = $('#clientSitesDoc').val();
    if (Clientsite == 'Select') {
        confirm('please select a clientsite')
    }
    else {
        const elem = $(this).find(":selected");
        if (elem.val() !== '') {
            const existing = $('#selectedDescDoc option[value="' + elem.val() + '"]');
            if (existing.length === 0) {
                $('#selectedDescDoc').append('<option value="' + elem.val() + '">' + elem.text() + '</option>');
                updateSelectedDescCount();
            }
        }
    }

});
function updateSelectedDescCount() {
    $('#selectedDescCountDoc').text($('#selectedDescDoc option').length);
}
$('#CrIsDownselect').on('change', function () {
    const isChecked = $(this).is(':checked');

    const filter = isChecked ? 1 : 2;
    if (filter == 1) {

        $('#IsDownselectonly').val(true)


    }
    if (filter == 2) {
        $('#IsDownselectonly').val(false)

    }
});
$('#btnSaveCriticalDoc').on('click', function () {
    $("input[name=clientSiteIds]").remove();
    var options = $('#selectedSitesDoc option');
    options.each(function () {
        const elem = '<input type="hidden" name="clientSiteIds" value="' + $(this).val() + '">';
        $('#frm_CriticalDoc').append(elem);
    });
    $("input[name=DescriptionIds]").remove();
    var optionsNew = $('#selectedDescDoc option');
    optionsNew.each(function () {
        const elem1 = '<input type="hidden" name="DescriptionIds" value="' + $(this).val() + '">';
        $('#frm_CriticalDoc').append(elem1);
    });
    var check = $('#frm_CriticalDoc').serialize();
    var Check1 = $('#clientSitesDoc').val();
    $.ajax({
        url: '/Admin/Settings?handler=SaveCriticalDocuments',
        type: 'POST',
        data: $('#frm_CriticalDoc').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (data) {
        if (data.success) {
            $('#Critical-modal').modal('hide');
            alert('Critical Document saved successfully');
            gridCriticalDocument.reload({ type: $('#sel_schedule').val() });
        } else {
            $('#CriDoc-modal-validation').html('');
            data.message.split(',').map(function (item) { $('#CriDoc-modal-validation').append('<li>' + item + '</li>') });
            $('#CriDoc-modal-validation').show().delay(5000).fadeOut();
        }
    });
});
let gridCriticalDocument;
gridCriticalDocument = $('#tbl_CriticalDocument').grid({
    dataSource: '/Admin/Settings?handler=CriticalDocumentList',
    uiLibrary: 'bootstrap4',
    iconsLibrary: 'fontawesome',
    primaryKey: 'id',
    columns: [
        {
            className: 'dt-control',
            orderable: false,
            data: null,
            width: '2%',
            defaultContent: '',
        },
        {
            field: 'groupName', title: 'Group Name', width: 70
        },
        /* { field: 'clientTypes', title: 'Client Types', width: 100 },*/
        { field: 'clientSites', title: 'Client Sites', width: 170 },
        {
            field: 'descriptions', title: 'Mandatory HR Documents', width: 230,
            renderer: function (value, record) {
                var html = '<table>';
                html += '<tbody>';
                html += '<tr><td style="width: 68px;">' + record.hrGroupName + '</td><td style="width: 40px;">' + record.referenceNO + '</td><td>' + record.descriptions + '</td></tr>';
                html += '</tbody>';
                html += '</table>';
                return html;
            }
        },
        { width: 80, renderer: schButtonRenderer },

    ],
    initialized: function (e) {
        $(e.target).find('thead tr th:last').addClass('text-center').html('<i class="fa fa-cogs" aria-hidden="true"></i>');
    }

});

function schButtonRenderer(value, record) {
    let buttonHtml = '';
    //buttonHtml += '<button class="btn btn-outline-primary mt-2 d-block" data-toggle="modal" data-target="#run-schedule-modal" data-sch-id="' + record.id + '""><i class="fa fa-play mr-2" aria-hidden="true"></i>Run</button>';
    buttonHtml += '<button class="btn btn-outline-primary mr-2 mt-2 d-block" data-toggle="modal" data-target="#Critical-modal" data-sch-id="' + record.id + '" ';
    buttonHtml += 'data-action="editSchedule"><i class="fa fa-pencil mr-2"></i>Edit</button>';
    buttonHtml += '<button class="btn btn-outline-danger mt-2 del-Cri d-block" data-sch-id="' + record.id + '""><i class="fa fa-trash mr-2" aria-hidden="true"></i>Delete</button>';
    return buttonHtml;
}
$('#tbl_CriticalDocument').on('click', '.del-Cri', function () {
    const idToDelete = $(this).attr('data-sch-id');
    if (confirm('Are you sure want to delete this Document?')) {
        $.ajax({
            url: '/Admin/Settings?handler=DeleteCriticalDoc',
            type: 'POST',
            data: { id: idToDelete },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function () {
            gridCriticalDocument.reload({ type: $('#sel_schedule').val() });
        });
    }

});

if ($('#hr_settings_fields_types').val() == '') {
    gridHrSettings.hide();
    gridLicenseTypes.hide();
    gridCriticalDocument.hide();
    gridLanguage.hide();
    gridHrSettingswithCourseLibrary.hide();
   
}

$('#Critical-modal').on('shown.bs.modal', function (event) {
    clearCriticalModal();
    const button = $(event.relatedTarget);
    const isEdit = button.data('action') !== undefined && button.data('action') === 'editSchedule';
    if (isEdit) {
        schId = button.data('sch-id');
        CriticalModelOnEdit(schId);
    } else {
        //scheduleModalOnAdd();
    }

    /*showHideSchedulePopupTabs(isEdit);*/
});
function clearCriticalModal() {
    $('#CriticalDocId').val('0');
    $('#CrIsDownselect').prop('checked', false);
    //$('#clientTypeNameDoc').html('');
    $('#clientTypeNameDoc').val('');
    $("#clientTypeNameDoc").multiselect("refresh");
    $('#clientSitesDoc').html('');
    $('#clientSitesDoc').val('');
    $("#clientSitesDoc").multiselect("refresh");
    $('#DescriptionDoc').html('<option value="">Select</option>');
    //$('#HRGroupDoc').html('<option value="">Select</option>');
    var valueToSelect = "Select";
    $('#HRGroupDoc').val(valueToSelect);
    $('#clientTypeNameDoc').val('');
    $('#selectedSitesDoc').html('');

    $('#selectedDescDoc').html('');
    $('#GroupName').val('');
    updateSelectedSitesCount();
    $('input:hidden[name="clientSiteIds"]').remove();

    $('#CriDoc-modal-validation').html('');


}
function CriticalModelOnEdit(CriticalDocId) {
    $('#loader').show();
    $.ajax({
        url: '/Admin/Settings?handler=CriticalDocList&id=' + CriticalDocId,
        type: 'GET',
        dataType: 'json',
    }).done(function (data) {
        $('#CriticalDocId').val(data.id);
        $('#GroupName').val(data.groupName);
        if (data.isCriticalDocumentDownselect == true) {
            $('#CrIsDownselect').prop('checked', true);
            $('#IsDownselectonly').val(true)

        }
        $.each(data.criticalDocumentsClientSites, function (index, item) {
            $('#selectedSitesDoc').append('<option value="' + item.clientSite.id + '">' + item.clientSite.name + '</option>');
            //$('#selectedDescDoc').append('<option value="' + item.hrSettings.id + '">' + item.hrSettings.description + '</option>');
            updateSelectedSitesCount();
        });
        $.each(data.criticalDocumentDescriptions, function (index, item) {
            $('#selectedDescDoc').append('<option value="' + item.hrSettings.id + '">' + item.hrSettings.referenceNoNumbers.name + item.hrSettings.referenceNoAlphabets.name + '&nbsp;&nbsp;&nbsp;' + item.hrSettings.description + '</option>');
            updateSelectedSitesCount();
        });

    }).always(function () {
        $('#loader').hide();
    });
}

$('#removeSelectedSites1').on('click', function () {
    $('#selectedSitesDoc option:selected').remove();
    updateSelectedSitesCount();
});
$('#removeSelectedSitesDoc').on('click', function () {
    $('#selectedDescDoc option:selected').remove();
    updateSelectedSitesCount();
});

//To save the Global Email Of Duress Button start
$('#add_GloblEmail').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var Email = $('#du_duress_email').val();
    var emailsArray = Email.split(',');
    var isValidEmailIds = true;
    for (var i = 0; i < emailsArray.length; i++) {
        var emailAddress = emailsArray[i].trim();
        if (isValidEmail(emailAddress)) {

        }
        else {
            isValidEmailIds = false;
            $.notify("Invalid email address.",
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );

        }

    }

    if (isValidEmailIds) {

        $.ajax({
            url: '/Admin/Settings?handler=SaveDuressEmail',
            data: { Email: Email },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            alert("The Duress Email Alert Email was saved successfully");
        })
    }



    function isValidEmail(email) {
        // Regular expression for basic email validation
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(email);
    }
})

$('#add_ComplianceEmail').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var Email = $('#hr_compliance_email').val();
    var emailsArray = Email.split(',');
    isValidEmailIds = true;
    for (var i = 0; i < emailsArray.length; i++) {
        var emailAddress = emailsArray[i].trim();
        if (isValidEmail(emailAddress)) {

        }
        else {
            isValidEmailIds = false;
            $.notify("Invalid email address." + emailAddress,
                {
                    align: "center",
                    verticalAlign: "top",
                    color: "#fff",
                    background: "#D44950",
                    blur: 0.4,
                    delay: 0
                }
            );

        }



    }

    if (isValidEmailIds) {
        $.ajax({
            url: '/Admin/Settings?handler=SaveGlobalComplianceAlertEmail',
            data: { Email: Email },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function () {
            alert("The Compliance Alert Email was saved successfully");
        })

    }

    function isValidEmail(email) {
        // Regular expression for basic email validation
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(email);
    }
})
$('#add_Dropbox').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var DroboxDir = $('#DropboxDir').val();
    $.ajax({
        url: '/Admin/Settings?handler=SaveDropboxDir',
        data: { DroboxDir: DroboxDir },
        type: 'POST',
        headers: { 'RequestVerificationToken': token },
    }).done(function (status) {
        alert("DropboxDirectory was saved successfully");
    })
})
//To save the Global Email Of Duress Button stop
//Timesheet save 
$('#add_Timesheet').on('click', function () {
    const token = $('input[name="__RequestVerificationToken"]').val();
    var weekname = $('#week').val();
    var frequency = $('#Reportfrequency').val();
    var mailid = $('#mailbox').val();
    var dropbox = $('#Defaultdropbox').val();
    var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$/;


    if (!emailPattern.test(mailid)) {
        alert("Please enter a valid email address.");
        return;
    }

    if (weekname) {

        $.ajax({
            url: '/Admin/Settings?handler=SaveTimesheet',
            data: { weekname: weekname, frequency: frequency, mailid: mailid, dropbox: dropbox },
            type: 'POST',
            headers: { 'RequestVerificationToken': token },
        }).done(function (status) {
            alert("Timesheet Details was saved successfully");
        });
    } else {
        alert("Please select a week.");
    }
})
//Timesheet stop
//p1-213 critical Document stop
$('#clientTypeNameDocHrDoc').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#clientSitesDocHrDoc').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});
$('#btn_save_hr_settings').on('click', function () {
    var form = document.getElementById('form_new_hr_settings');
    var jsformData = new FormData(form);
    var data = $('#list_hrGroups').val();

    var SelectedStates = $('#HrState').val();

    var allSitesValues = $('#selectedSitesDocHrDoc option');
    var allValues = [];

    allSitesValues.each(function () {
        allValues.push($(this).val());
    });


    if ($('#list_hrGroups').val() == '') {
        alert('Please Select HrGroups')
    }
    else if ($('#list_ReferenceNoNumber').val() == '' || $('#list_ReferenceNoAlphabet').val() == '') {
        alert('Please Select  Reference Numbers')
    }
    else {
        $.ajax({
            url: '/Admin/GuardSettings?handler=SaveHRSettings',
            type: 'POST',
            //data: jsformData,
            data: {
                'Id': $('#HrSettings_Id').val(),
                'hrGroupId': $('#list_hrGroups').val(),
                'refNoNumberId': $('#list_ReferenceNoNumber').val(),
                'refNoAlphabetId': $('#list_ReferenceNoAlphabet').val(),
                'description': $('#txtHrSettingsDescription').val(),
                'Selectedsites': allValues,
                'SelectedStates': SelectedStates
            },
            //processData: false,
            //contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            beforeSend: function () {
                $('#loader').show();
            }
        }).done(function (result) {
            if (result.status) {


                $('#hrSettingsModal').modal('hide');
                gridHrSettings.clear();
                gridHrSettings.reload();
                gridHrSettingswithCourseLibrary.clear();
                gridHrSettingswithCourseLibrary.reload();
            } else {
                displayValidationSummaryHrSettings(result.errors);
            }
        }).always(function () {
            $('#loader').hide();
        });

    }

});
function displayValidationSummaryHrSettings(errors) {
    const summaryDiv = document.getElementById('hrsettings-field-validation');
    summaryDiv.className = "validation-summary-errors";
    summaryDiv.querySelector('ul').innerHTML = '';
    errors.forEach(function (item) {
        const li = document.createElement('li');
        li.appendChild(document.createTextNode(item));
        summaryDiv.querySelector('ul').appendChild(li);
    });
}
//p1-213 document step L Start 
$('#clientTypeNameDocHrDoc').on('change', function () {
    let clientTypeIds = $(this).val().join(';');
    const option = clientTypeIds;
    $('#clientSitesDocHrDoc').html('');
    const clientSiteControl = $('#clientSitesDocHrDoc');

    //if (option === '') {
    //    $('#clientSitesDocHrDoc').html('');
    //    $('#clientSitesDocHrDoc').append('<option value="">Select</option>');
    //}

    $.ajax({
        url: '/admin/settings?handler=ClientSitesNew1',
        type: 'GET',
        data: {
            typeId: option

        },
    }).done(function (data) {
        data.map(function (site) {
            $('#clientSitesDocHrDoc').append('<option value="' + site.id + '">' + site.name + '</option>');
        });
        clientSiteControl.multiselect('rebuild');

    });
});

$('#clientSitesDocHrDoc').on('change', function () {
    const selectedValues = $(this).val().join(';').split(';');
    selectedValues.forEach(function (value) {
        if (value !== '') {
            const existing = $('#selectedSitesDocHrDoc option[value="' + value + '"]');
            if (existing.length === 0) {
                const text = $('#clientSitesDocHrDoc option[value="' + value + '"]').text();
                $('#selectedSitesDocHrDoc').append('<option value="' + value + '">' + text + '</option>');
            }
        }
    });
    updateSelectedSitesCountHrDoc();

});
function updateSelectedSitesCountHrDoc() {
    $('#selectedSitesCountDocHrDoc').text($('#selectedSitesDocHrDoc option').length);

}
$('#removeSelectedSitesHrDoc').on('click', function () {
    $('#selectedSitesDocHrDoc option:selected').remove();
    updateSelectedSitesCountHrDoc();
});


function clearCriticalModalHrDoc() {

    $('#scheduleId').val('0');
    $('#clientTypeNameDocHrDoc').val('');
    $('#clientSitesDocHrDoc').val('');
    $('#selectedSitesDocHrDoc').html('');
    updateSelectedSitesCountHrDoc();
    $('#clientTypeNameDocHrDoc option:eq(0)').attr('selected', true);





}


$('#hrSettingsModal').on('shown.bs.modal', function (event) {
    clearCriticalModalHrDoc();

    /*showHideSchedulePopupTabs(isEdit);*/
});

$('#HrState').multiselect({
    maxHeight: 400,
    buttonWidth: '100%',
    nonSelectedText: 'Select',
    buttonTextAlignment: 'left',
    includeSelectAllOption: true,
});



//p1-213 document step L end

$("#btnDownloadClientSiteExcel").click(async function () {

    const currentDateTime = new Date().toISOString().split('T')[0];
    const fileName = `ClientSites - ${currentDateTime}.xlsx`;



    $('#loader').show(); // Show loader


    try {
        // Fetch data from the server
        const response = await $.ajax({
            url: '/Admin/Settings?handler=ClientSitesExcel',
            type: 'GET',
            dataType: "json",
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            data: {
                typeId: null,
            }
        });

        $('#loader').hide(); // Hide loader
        console.log(response.data);

        //const filteredData = response.data.filter(item => guardIds.includes(item.id));
        // Ensure response contains data
        const rawData = Array.isArray(response) ? response : [];
        if (rawData.length === 0) {
            console.error("No data available to export.");
            alert("No data available to export.");
            return;
        }

        // Define headers and column widths

        let maxSmartWands = 0;

        rawData.forEach(item => {
            const smartWands = Array.isArray(item.smartWands) ? item.smartWands : [];
            if (smartWands.length > maxSmartWands) {
                maxSmartWands = smartWands.length;
            }
        });

        // Step 2: Define base headers and dynamic SmartWand headers
        const baseHeaders = [
            'Client Type',
            'Client Site',
            'Emails',
            'Address',
            'State',
            'GPS',
            'Billing',
            'Status',
            'Account Manager'
        ];

        // Dynamically add SmartWand and SIMProvider headers
        const smartWandHeaders = [];
        for (let i = 1; i <= maxSmartWands; i++) {
            smartWandHeaders.push(`SmartWand${i}`, `SIMProvider`);
        }

        const headers = [...baseHeaders, ...smartWandHeaders];
        const columnWidths = [20, 20, 10, 10, 20, 20, 20, 25, 25]; // Example widths




        const ws = XLSX.utils.aoa_to_sheet([[]]);



        // Create the data rows
        //const dataRows = [headers, ...rawData.map(item => [
        //    item.clientType,
        //    item.name,
        //    item.emails,
        //    item.address,
        //    item.state,
        //    item.gps,
        //    item.billing,
        //    item.status,

        //])];
        const dataRows = [
            headers, // Add headers as the first row
            ...rawData.map(item => {
                const clientSite = item.clientSite || {};
                const clientType = clientSite.clientType?.name || '';
                const smartWands = Array.isArray(item.smartWands) ? item.smartWands : [];

                // Base data for the row
                const rowData = [
                    clientType,
                    clientSite.name || '',
                    clientSite.emails || '',
                    clientSite.address || '',
                    clientSite.state || '',
                    clientSite.gps || '',
                    clientSite.billing || '',
                    clientSite.status === 0 ? 'Ongoing' :
                        clientSite.status === 1 ? 'Expiring' :
                            clientSite.status === 2 ? 'Expired' : '',
                    clientSite.accountManager
                ];

                // Add SmartWand and SIMProvider data dynamically
                for (let i = 0; i < maxSmartWands; i++) {
                    const smartWand = smartWands[i] || {}; // Use an empty object if no smart wand exists
                    rowData.push(smartWand.phoneNumber || '', smartWand.simProvider || '');
                }

                return rowData;
            })
        ];


        // Update the worksheet with headers and data
        XLSX.utils.sheet_add_aoa(ws, dataRows);

        // Set column widths
        ws['!cols'] = columnWidths.map(width => ({ wch: width }));

        // Create a new workbook and append the worksheet
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, 'ClientSites');

        // Write the file
        XLSX.writeFile(wb, fileName);
    } catch (error) {
        $('#loader').hide(); // Hide loader in case of error
        console.error('Error fetching or processing data:', error); // Log error
        alert("An error occurred while exporting data.");
    }
});


$('#btnAIButton').on('click', function () {
    if ($('#Report_Feedback').val() != '') {
        const userInput = $("#Report_Feedback").val();
        var obj = {
            Text: userInput
        }
        //$.ajax({
        //    url: "https://api.languagetool.org/v2/check",
        //    method: "POST",
        //    data: {
        //        text: userInput,
        //        language: "en-US"
        //    },
        //    success: function (data) {
        //        //console.log(data.matches);
        //        if (data.matches.length > 0) { 
        //            let originalText = userInput;
        //            $.each(data.matches, function (index, item) {



        //            let correctedText = originalText;
        //            // Apply the first correction from LanguageTool
        //            let match = item;
        //            let start = match.offset;
        //            let length = match.length;
        //            let replacement = match.replacements[0].value;
        //            // Replace the incorrect part with the suggested correction
        //            correctedText =
        //                correctedText.substring(0, start) +
        //                replacement +
        //                    correctedText.substring(start + length);
        //                originalText = correctedText;
        //            });
        //            $("#Report_Feedback").val(originalText);
        //            }
        //    }
        //});
        $.ajax({
            url: "/Incident/Register?handler=AiButton", // Your C# endpoint
            method: "GET",
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            contentType: "application/json",
            data: { 'textToCheck': userInput },
            success: function (response) {
                const correctedText = response.truckConfigText.result;
                $("#Report_Feedback").val(correctedText);

            },
            error: function (xhr, status, error) {
                console.error("Error:", status, error);
                $("#correctedOutput").text("An error occurred.");
            }
        });
    } else {
        alert("Please Enter The Feedback")
    }
});




