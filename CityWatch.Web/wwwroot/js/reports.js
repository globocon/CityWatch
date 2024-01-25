$(function () {

    /** Patrol Data Report ***/
    function loadDefaultDates() {
        const today = new Date();
        const start = new Date(today.getFullYear(), today.getMonth(), 2);
        $('#ReportRequest_FromDate').val(start.toISOString().substr(0, 10));
        $('#ReportRequest_ToDate').val(today.toISOString().substr(0, 10));
    }

    loadDefaultDates();

    var patrolReport = $('#monthly_patrol_data').DataTable({
        lengthMenu: [[75, 100, -1], [75, 100, "All"]],
        pageLength: 100,
        paging: true,
        searching: true,
        ordering: false,
        info: false,
        scrollX: true,
        data: [],
        columns: [
            { data: 'nameOfDay' },
            { data: 'date' },
            { data: 'serialNo' },
           // { data: 'fileNametodownload' },
            {
                data: 'fileNametodownload', 
                render: function (data, type, row) {
                    if (data) {
                        return '<a href="https://c4istorage1.blob.core.windows.net/irfiles/' + data.substring(0, 8) + '/' + data + '"target="_blank"><img src="/images/pdfimage.jpg" style="width:115%" alt="Image"></a>';
                    } else {
                        return ''; 
                    }
                }
            },
            { data: 'controlRoomJobNo' },
            { data: 'siteName' },
            { data: 'siteAddress' },
            { data: 'despatchTime' },
            { data: 'arrivalTime' },
            { data: 'departureTime' },
            { data: 'serialNo' },
            { data: 'totalMinsOnsite' },
            { data: 'responseTime' },
            { data: 'alarm' },
            { data: 'patrolAttented' },
            { data: 'actionTaken' },
            { data: 'notifiedBy' },
            { data: 'billing' }
        ],
        'createdRow': function (row, data, index) {
            // alarm
            $('td', row).eq(11).addClass('alarm');
            // action taken
            $('td', row).eq(13).addClass('action-taken');
        }
    });

    $('#ReportRequest_DataFilter').on('change', function () {
        const reportType = $(this).val();
        if (reportType === '2')
            $('#patrol_report_controls').show();
        else
            $('#patrol_report_controls').hide();
        $('#ReportRequest_ClientType option:first').prop('selected', true);
        $('#ReportRequest_ClientSites option:first').prop('selected', true);
        $('#ReportRequest_Position option:first').prop('selected', true);
    });

    $('#ReportRequest_ClientTypes').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'All',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    $('#ReportRequest_ClientSites').multiselect({
        maxHeight: 400,
        buttonWidth: '100%',
        nonSelectedText: 'All',
        buttonTextAlignment: 'left',
        includeSelectAllOption: true,
    });

    $('#ReportRequest_ClientTypes').on('change', function () {
        const clientType = $(this).val().join(';');
        const clientSiteControl = $('#ReportRequest_ClientSites');
        clientSiteControl.html('');

        $.ajax({
            url: '/Reports/PatrolData?handler=ClientSites&types=' + encodeURIComponent(clientType),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                data.map(function (site) {
                    clientSiteControl.append('<option value="' + site.text + '">' + site.text + '</option>');
                });
                clientSiteControl.multiselect('rebuild');
            }
        });
    });

    $('#btnPatrolReportSumbit').on('click', function () {
        $('#btnExportExcel').attr('href', '#');
        const fromDate = $('#date_from').val();
        const toDate = $('#date_to').val();
        if (fromDate === '' || toDate === '') {
            alert('From date and to date is required');
            return false;
        }
         //calculate month difference-start
        var date1 = new Date($('#ReportRequest_FromDate').val());
        var date2 = new Date($('#ReportRequest_ToDate').val());
        var monthdiff = monthDiff(date1, date2); 
        if (monthdiff > 12) {
            alert('Date Range is  greater than 12 months');
            return false;
        }
        //calculate month difference-end
        $('#loader-p').show();
        $.ajax({
            url: '/Reports/PatrolData?handler=GenerateReport',
            type: 'POST',
            dataType: 'json',
            data: $('#frm_patrol_report_request').serialize(),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        }).done(function (response) {
            patrolReport.clear().rows.add(response.results).draw();
           
            drawPieChart(response.chartData.sitePercentage, response.recordCount, "svg#pie_chart_ir_by_site");
            drawPieChart(response.chartData.areaWardPercentage, response.recordCount, "svg#pie_chart_ir_by_areaward");
            drawPieChart(response.chartData.colorCodePercentage, response.recordCount, "svg#pie_chart_ir_by_colorcode")
            drawPieChart(response.chartData.eventTypePercentage, response.recordCount, "svg#pie_chart_by_ireventype_quantity");
            drawBarChart(response.chartData.eventTypeCount, response.recordCount, "svg#bar_chart_by_ireventype_quantity");
            $('#btnExportExcel').attr('href', '/Reports/PatrolData?handler=DownloadReport&file=' + response.fileName);
            $('#count_by_site').html(response.chartData.sitePercentage.length);
            $('#count_by_area_ward').html(response.chartData.areaWardPercentage.length);
            $('#count_color_code').html(response.chartData.colorCodePercentage.length);            
            $('#count_by_ir').html(response.chartData.eventTypeCount.map(x => x.value).reduce((f, s) => f + s, 0));

            /* expanding grapph - start*/
            drawPieChart(response.chartData.sitePercentage, response.recordCount, "svg#pie_chart_ir_by_site1");
            drawPieChart(response.chartData.areaWardPercentage, response.recordCount, "svg#pie_chart_ir_by_areaward1");
            drawPieChart(response.chartData.colorCodePercentage, response.recordCount, "svg#pie_chart_ir_by_colorcode1")
            $('#count_by_site1').html(response.chartData.sitePercentage.length);
            $('#count_by_area_ward1').html(response.chartData.areaWardPercentage.length);
            $('#count_color_code1').html(response.chartData.colorCodePercentage.length);   
            /* expanding grapph - start*/
        }).fail(function () {
        }).always(function () {
            $('#loader-p').hide();
        });
    });
    
    /** Patrol Data Charts ***/

    /****************************************************************************************
    *  IMPORTANT: This is a copy of drawPieChart() in CityWatch.KPI\Scripts\ir-chart.js
    *  Any changes - should be done in both places
    *****************************************************************************************/
    const d3Colors = d3.scaleOrdinal([...d3.schemeCategory10, ...d3.schemeAccent]);

    const colorCodes = [
        { 'name': 'red', 'color': '#ff0000' },
        { 'name': 'yellow', 'color': '#ffff00' },
        { 'name': 'green', 'color': '#00ff00' },
        { 'name': 'n/a', 'color': '#4682b4' },
    ];

    const truncate = (value = '', maxLength = 25) =>
        value.length > maxLength
            ? `${value.substring(0, maxLength)}…`
            : value;

    function getFillColor(d, i, key) {
        let colorFound = colorCodes.find(function (item) {
            return key && key.toLowerCase().includes(item.name);
        });

        if (colorFound) return colorFound.color;
        
        return d3Colors(i);
    }

    function drawPieChart(data, recordCount, control) {

        $(control).html('');

        if (recordCount === 0) return;

        var svg = d3.select(control),
            width = svg.attr('width'),
            height = svg.attr('height'),
            radius = Math.min(width, height) / 2,
            arcX = width / 4,
            arcY = height / 2,
            legendX = (width / 2) + 5,
            g = svg.append('g').attr('transform', 'translate(' + arcX + ',' + arcY + ')');

        // Generate the pie
        var pie = d3.pie()
            .value(function (d) { return d.value; });

        // Generate the arcs
        var arc = d3.arc()
            .innerRadius(0)
            .outerRadius(radius);

        //Generate groups
        var arcs = g.selectAll('arc')
            .data(pie(data))
            .enter()
            .append('g')
            .attr('class', 'arc');

        //Draw arc paths
        arcs.append('path')
            .attr('fill', function (d, i) { return getFillColor(d, i, data[i].key); })
            .attr('d', arc);

        //Append values on chart
        arcs.append('text')
            .attr('transform', function (d) { return 'translate(' + arc.centroid(d) + ')'; })
            .style('font-size', "11px")
            .style("font-family", "Arial")
            .attr('text-anchor', 'middle')
            .text(function (d, i) {
                if (data[i].value > 0)
                    return data[i].value + '%';
            });

        //Generate legend
        var legend = svg.selectAll('legend')
            .data(pie(data))
            .enter()
            .append('g')
            .attr('transform', function (d, i) { return 'translate(' + legendX + ',' + (i * 15 + 20) + ')'; });

        //Append legend box
        legend.append('rect')
            .attr('width', 10)
            .attr('height', 10)
            .attr('fill', function (d, i) { return getFillColor(d, i, data[i].key); });

        //Append legend text
        legend.append('text')
            .text(function (d, i) { return truncate(data[i].key) + " (" + data[i].value + "%)"; })
            .style("font-size", "11px")
            .style("font-family", "Arial")
            .attr("x", 12)
            .attr("y", 8);
    }

    /****************************************************************************************
    *  IMPORTANT: This is a copy of drawBarChart() in CityWatch.KPI\Scripts\ir-chart.js
    *  Any changes - should be done in both places
    *****************************************************************************************/
    function drawBarChart(data, recordCount, control) {

        $(control).html('');

        if (recordCount === 0) return;

        // set the dimensions and margins of the graph
        var margin = { top: 20, right: 30, bottom: 40, left: 100 },
            width = 460 - margin.left - margin.right,
            height = 400 - margin.top - margin.bottom;

        var svg = d3.select(control)
            .attr('width', width + margin.left + margin.right)
            .attr('height', height + margin.top + margin.bottom)
            .append('g')
            .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

        // add X axis
        var x = d3.scaleLinear()
            .domain([0, d3.max(data, d => d.value)])
            .range([0, width]);

        svg.append('g')
            .attr('transform', 'translate(0,' + height + ')')
            .call(d3.axisBottom(x))
            .selectAll('text')
            .attr("font-size", "11px")
            .attr("font-family", "Arial")
            .attr('transform', 'translate(-10,0)rotate(-45)')
            .style('text-anchor', 'end');

        //label x-axis Quantity
        svg.append('text')
            .attr('transform', 'translate(100,0)')
            .attr('x', 160)
            .attr('y', 375)
            .attr('font-size', '11px')
            .attr("font-family", "Arial")
            .text('Quantity');

        // Y axis
        var y = d3.scaleBand()
            .range([0, height])
            .domain(data.map(function (d) { return d.key; }))
            .padding(.1);

        svg.append('g').call(d3.axisLeft(y));

        //label y-axis Event Type
        svg.append('text')
            .attr('transform', 'translate(-10,0)rotate(-90)')
            .attr('x', -90)
            .attr('y', -20)
            .attr('font-size', '11px')
            .attr("font-family", "Arial")
            .text('Event Type');

        // bars
        svg.selectAll('bar')
            .data(data)
            .enter()
            .append('rect')
            .attr('x', x(0))
            .attr('y', function (d) { return y(d.key); })
            .attr('width', function (d) { return x(d.value); })
            .attr('height', y.bandwidth())
            .attr('fill', '#00468b')

        // values in bar chart
        svg.selectAll('text.bar')
            .data(data)
            .enter().append('text')
            .attr("font-size", "11px")
            .attr("font-family", "Arial")
            .attr('x', function (d) { return x(d.value) + 8; })
            .attr('y', function (d) { return y(d.key) + 12; })
            .attr('width', function (d) { return x(d.value); })
            .attr('height', y.bandwidth())
            .text(function (d, i) {
                if (data[i].value > 0)
                    return data[i].value
            });
    }
});
 //calculate month difference-start

function monthDiff(d1, d2) {
    var months;
    months = (d2.getFullYear() - d1.getFullYear()) * 12;
    months -= d1.getMonth();
    months += d2.getMonth();
    return months <= 0 ? 0 : months;
}
$('#btncount_by_site').on('click', function () {
    $('#modelIRRecordsbySiteGraph').modal('show');
});
$('#btncount_by_area_ward').on('click', function () {
    $('#modelIRRecordsbyAreaWardGraph').modal('show');
});
$('#btncount_color_code').on('click', function () {
    $('#modelIRRecordsbyColorCodeGraph').modal('show');
});
 //calculate month difference-end