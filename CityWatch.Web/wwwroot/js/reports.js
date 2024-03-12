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
    window.myChart1;
    window.myChart2;
    window.myChart3;
    window.myChart4;
    $('#btnPatrolReportSumbit').on('click', function () {

        if (window.myChart1 != undefined)
            window.myChart1.destroy();
        if (window.myChart2 != undefined)
            window.myChart2.destroy();
        if (window.myChart3 != undefined)
            window.myChart3.destroy();
        if (window.myChart4 != undefined)
            window.myChart4.destroy();

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
            /*drawPieChart(response.chartData.areaWardPercentage, response.recordCount, "svg#pie_chart_ir_by_areaward");*/
            /*drawPieChart(response.chartData.colorCodePercentage, response.recordCount, "svg#pie_chart_ir_by_colorcode")*/
            drawPieChart(response.chartData.eventTypePercentage, response.recordCount, "svg#pie_chart_by_ireventype_quantity");
            drawBarChart(response.chartData.eventTypeCount, response.recordCount, "svg#bar_chart_by_ireventype_quantity");
            $('#btnExportExcel').attr('href', '/Reports/PatrolData?handler=DownloadReport&file=' + response.fileName);
            $('#count_by_site').html(response.chartData.sitePercentage.length);
            $('#count_by_area_ward').html(response.chartData.areaWardPercentage.length);
            $('#count_color_code').html(response.chartData.colorCodePercentage.length);
            $('#count_by_ir').html(response.chartData.eventTypeCount.map(x => x.value).reduce((f, s) => f + s, 0));

            /* expanding grapph - start*/
            drawPieChartLargeSize(response.chartData.sitePercentage, response.recordCount, "svg#pie_chart_ir_by_site1");
            /* drawPieChartLargeSize(response.chartData.areaWardPercentage, response.recordCount, "svg#pie_chart_ir_by_areaward1");*/
            /* drawPieChartLargeSize(response.chartData.colorCodePercentage, response.recordCount, "svg#pie_chart_ir_by_colorcode1");*/
            drawPieChartLargeSize(response.chartData.eventTypePercentage, response.recordCount, "svg#pie_chart_by_ireventype_quantity1");
            $('#count_by_site1').html(response.chartData.sitePercentage.length);
            $('#count_by_area_ward1').html(response.chartData.areaWardPercentage.length);
            $('#count_color_code1').html(response.chartData.colorCodePercentage.length);
            $('#txtDownloadfilename').val(response.fileName2)

            drawPieChartUsingChartJsChart(response.chartData.areaWardPercentage);
            drawPieChartUsingChartJsChartColorCode(response.chartData.colorCodePercentage);
            /* expanding grapph - start*/
        }).fail(function () {
        }).always(function () {
            $('#loader-p').hide();
        });
    });


    /************Chart in popup 13/12/2024 large Size Start ***************** */

    function drawPieChartLargeSize(data, recordCount, control) {

        $(control).html('');

        if (recordCount === 0) return;

        var svg = d3.select(control),
            width = svg.attr('width'),
            height = svg.attr('height'),
            radius = Math.min(width, height - 60) / 2,
            arcX = (width / 2) - 150,
            arcY = (height / 2),
            legendX = (width / 2) + 205,
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
            .attr('text-anchor', 'middle')
            .text(function (d, i) {
                if (data[i].value > 0)
                    return data[i].value + '%';
            });



        // Draw labels outside the slices
        var labels = arcs.append("text")
            .attr("transform", function (d) {
                var pos = arc.centroid(d);
                var midAngle = Math.atan2(pos[1], pos[0]);
                var x = Math.cos(midAngle) * (radius + 10);
                var y = Math.sin(midAngle) * (radius + 10);
                return "translate(" + x + "," + y + ")";
            })
            .attr("dy", "0.35em")
            .attr("text-anchor", function (d) {
                return (d.startAngle + d.endAngle) / 2 > Math.PI ? "end" : "start";
            })
            .text(function (d) { return d.data.label; });

        // Draw leader lines
        /*  arcs.append("line")
              .attr("stroke", "black")
              //.attr("x1", function (d) { return arc.centroid(d)[0]; })
              .attr("x1", function (d) {
                  var a = d.startAngle + (d.endAngle - d.startAngle) / 2 - Math.PI / 2;
                  d.cx = Math.cos(a) * (radius - 1);
                  return d.x = Math.cos(a) * (radius - 1);
              })
              // .attr("y1", function (d) { return arc.centroid(d)[1]; })
              .attr("y1", function (d) {
                  var a = d.startAngle + (d.endAngle - d.startAngle) / 2 - Math.PI / 2;
                  d.cy = Math.sin(a) * (radius - 75);
                  return d.y = Math.sin(a) * (radius - 1);
  
              })
              .attr("x2", function (d) {
                  var pos = arc.centroid(d);
                  var midAngle = Math.atan2(pos[1], pos[0]);
                  var x = Math.cos(midAngle) * (radius + 20);
                  return x;
              })
              .attr("y2", function (d) {
                  var pos = arc.centroid(d);
                  var midAngle = Math.atan2(pos[1], pos[0]);
                  var y = Math.sin(midAngle) * (radius + 20);
                  return y;
              });*/




        arcs.select("text")
            .attr("transform", function (d) {
                var centroid = arc.centroid(d),
                    x = centroid[0],
                    y = centroid[1],
                    h = Math.sqrt(x * x + y * y);
                var angle = (d.startAngle + d.endAngle) / 2;
                return "translate(" + (x / h * (radius + 1)) + ',' + (y / h * (radius + 1)) + ") rotate(" + (angle * 180 / Math.PI - 90) + ")";
                /*return "translate(" + (x / h * (radius + 20)) + ',' + (y / h * (radius + 20)) + ")";*/
            })
            .style("text-anchor", function (d) {
                return (d.endAngle + d.startAngle) / 2 > Math.PI ? "start" : "start";
            });


        //Generate legend
        var legend = svg.selectAll("legend")
            .data(pie(data))
            .enter()
            .append("g")
            .attr("transform", function (d, i) { return "translate(" + legendX + "," + (i * 12 + 3) + ")"; });

        //Append legend box
        legend.append("rect")
            .attr("width", 8)
            .attr("height", 8)
            .attr("fill", function (d, i) { return getFillColor(d, i, data[i].key); });

        //Append legend text
        legend.append("text")
            .text(function (d, i) { return data[i].key + " (" + data[i].value + "%)"; })
            .style("font-size", "10px")
            .attr("x", 11)
            .attr("y", 8);





    }

    /***************************** end  */
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

$('#btncount_event_type_quantity').on('click', function () {
    $('#modelIreventypeQuantity').modal('show');
});
//calculate month difference-end
$('#btnExportPatrolPdf').on('click', function () {
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
        url: '/Reports/PatrolData?handler=GeneratePdfReport',
        type: 'POST',
        dataType: 'json',
        data: $('#frm_patrol_report_request').serialize(),
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    }).done(function (response) {
        const pdfName = response.fileName !== '' ? '/Pdf/Output/' + response.fileName : '#';
        window.open(pdfName, '_target/')
    }).fail(function () {
    }).always(function () {
        $('#loader-p').hide();
    });
});


function drawPieChartUsingChartJsChart(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.key;
    });
    var data2 = dataValue.map(function (e) {
        return e.value;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("pie_chart_ir_by_areaward");
    var canvas2 = document.getElementById("pie_chart_ir_by_areaward1");
    if (canvas !== null) {
        const ctx = document.getElementById('pie_chart_ir_by_areaward').getContext('2d');
       
        window.myChart1 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]}%)`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });

       
    }



    if (canvas2 !== null) {
        const ctx = document.getElementById('pie_chart_ir_by_areaward1').getContext('2d');
        window.myChart2 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]}%)`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }
    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}



function drawPieChartUsingChartJsChartColorCode(dataValue) {

    var labels = dataValue.map(function (e) {
        return e.key;
    });
    var data2 = dataValue.map(function (e) {
        return e.value;
    });
    // Data for the pie chart
    const data = {
        labels: labels,
        datasets: [{
            data: data2, // Values for each slice

        },
        ],
        datalabels: {
            // display labels for this specific dataset
            display: true
        }
    };


    var canvas = document.getElementById("pie_chart_ir_by_colorcode");
    var canvas2 = document.getElementById("pie_chart_ir_by_colorcode1");
    if (canvas !== null) {
        const ctx = document.getElementById('pie_chart_ir_by_colorcode').getContext('2d');
        window.myChart3 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 10,
                        right: 10,
                        top: 2,
                        bottom: 2
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {

                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]}%)`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },

                        outsidePadding: 4,
                        textMargin: 4

                    },

                }

            },


        });
    }



    if (canvas2 !== null) {
        const ctx = document.getElementById('pie_chart_ir_by_colorcode1').getContext('2d');
        window.myChart4 = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    label: '# of Votes',
                    data: data2,
                    backgroundColor: getColors(15),
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                layout: {
                    padding: {
                        left: 50,
                        right: 5,
                        top: 20,
                        bottom: 20
                    }
                },
                maintainAspectRatio: false,
                plugins: {
                    tooltip: {
                        enabled: true,
                        callbacks: {
                            label: function (context) {
                                let label = context.label + '(' + context.formattedValue + '%)'
                                return label;
                            }
                        }
                    },
                    legend: {
                        position: 'right',
                        labels: {
                            font: {
                                family: 'Arial',
                                size: 11
                            },

                            boxWidth: 10,
                            boxHeight: 10,
                            generateLabels(chart) {
                                const data = chart.data;
                                if (data.labels.length && data.datasets.length) {
                                    const { labels: { pointStyle } } = chart.legend.options;

                                    return data.labels.map((label, i) => {
                                        const meta = chart.getDatasetMeta(0);
                                        const style = meta.controller.getStyle(i);

                                        return {
                                            text: `${label} (${data['datasets'][0].data[i]}%)`,
                                            fillStyle: style.backgroundColor,
                                            strokeStyle: style.borderColor,
                                            lineWidth: style.borderWidth,
                                            borderWidth: 0,
                                            pointStyle: pointStyle,
                                            hidden: !chart.getDataVisibility(i),

                                            // Extra data used for toggling the correct item
                                            index: i
                                        };
                                    });
                                }
                                return [];
                            }
                        }
                    },
                    labels: {
                        /* render:"value",*/
                        render: (args) => {

                            return args.value + '%';

                        },
                        position: 'outside',
                        outsidePadding: 10,
                        textMargin: 10

                    },

                }

            },


        });
    }
    function getColors(length) {
        let pallet = ["#4682b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b", "#e377c2",
            "#7f7f7f", "#bcbd22", "#17becf",
            "#85144b", "#F012BE", "#3D9970", "#111111", "#AAAAAA"];
        let colors = [];

        for (let i = 0; i < length; i++) {
            colors.push(pallet[i % (pallet.length - 1)]);
        }

        return colors;
    }
    //// Configuration options
    //const options = {
    //    responsive: false,
    //    plugins: {
    //        legend: {
    //            position: 'right',
    //            onClick: function (event, legendItem) {
    //                const index = legendItem.index;
    //                const meta = this.chart.getDatasetMeta(0);
    //                meta.data[index].hidden = !meta.data[index].hidden;
    //                this.chart.update();
    //            }
    //        },
    //        datalabels: {
    //            backgroundColor: function (context) {
    //                return context.dataset.backgroundColor;
    //            },
    //            borderColor: 'white',
    //            borderRadius: 25,
    //            borderWidth: 2,
    //            color: 'white',
    //            display: function (context) {
    //                var dataset = context.dataset;
    //                var count = dataset.data.length;
    //                var value = dataset.data[context.dataIndex];
    //                return value > count * 1.5;
    //            }
    //        }
    //    }

    //};

    //// Create the pie chart
    //var canvas = document.getElementById("myPieChart");
    //if (canvas !== null) {
    //    const ctx = document.getElementById('myPieChart').getContext('2d');
    //    const myPieChart = new Chart(ctx, {
    //        type: 'pie',
    //        data: data,
    //        options: options
    //    });

    //}

}