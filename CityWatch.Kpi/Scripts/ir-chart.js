const d3 = require("d3");
const jsdom = require("jsdom");
const { createConverter } = require('convert-svg-to-png');
const fs = require('fs');

/****************************************************************************************
*  Shared SVG->PNG converter. The library's one-shot convert() launches and destroys a
*  whole headless Chromium per call (~1-3s each); a report has ~13 charts, so that was
*  the bulk of the PDF generation time. This module is cached by the NodeJS host, so
*  one converter (one browser) is reused across every chart of every report.
*  Calls are queued because a Converter is not safe for concurrent use, and a broken
*  converter (e.g. the browser process died) is replaced and retried once per call.
*  Same helper exists in effort-chart.js - keep the two in step.
*****************************************************************************************/
let sharedConverter = null;
let convertQueue = Promise.resolve();

function convertSvgToPng(html) {
    const result = convertQueue.then(async () => {
        if (!sharedConverter || sharedConverter.destroyed)
            sharedConverter = createConverter();
        try {
            return await sharedConverter.convert(html);
        } catch (e) {
            try { await sharedConverter.destroy(); } catch (ignored) { }
            sharedConverter = createConverter();
            return await sharedConverter.convert(html);
        }
    });
    convertQueue = result.catch(() => { });
    return result;
}

function drawChart(callback, options, data) {

    const { JSDOM } = jsdom;
    const { document } = (new JSDOM('')).window;
    global.document = document;

    const d3Colors = d3.scaleOrdinal([...d3.schemeCategory10, ...d3.schemeAccent]);
    const colorCodes = [
        { 'name': 'red', 'color': '#ff0000' },
        { 'name': 'yellow', 'color': '#ffff00' },
        { 'name': 'green', 'color': '#00ff00' },
        { 'name': 'n/a', 'color': '#4682b4' },
        { 'name': 'no/data', 'color': '#FFFFFF' },
    ]

    const getFillColor = (d, i, key) => {
        let colorFound = colorCodes.find(function (item) {            
            return key && key.toLowerCase().includes(item.name);
        });
        if (colorFound) return colorFound.color;

        return d3Colors(i);
    }

    const truncate = (value = '', maxLength = 25) =>
        value.length > maxLength
            ? `${value.substring(0, maxLength)}…`
            : value;

    var body = d3.select(document).select("body");

    // "||" not "|": the old bitwise OR turned width 800 into 1012 (800|500). The bar and
    // column charts overwrite the svg width themselves, but drawPieChart READS it to lay
    // out the pie and legend, so it must be the real requested width.
    body.append("svg")
        .attr("width", options.width || 500)
        .attr("height", 320);

    // 1 = Pie chart 2 = Bar chart 3 = Column chart (vertical, time-ordered)
    switch (options.type) {
        case 1:
            drawPieChart(data);
            break;
        case 2:
            drawBarChart(data)
            break;
        case 3:
            drawColumnChart(data, options.width)
            break;
    }

    convertSvgToPng(body.node().innerHTML)
        .then(buffer => fs.writeFile(options.fileName, buffer, () => callback(null, "OK")))
        .catch(e => console.error(e));

    /****************************************************************************************
    *  IMPORTANT: This is a copy of drawPieChart() in CityWatch.Web\wwwroot\js\report.js
    *  Any changes - should be done in both places
    *****************************************************************************************/    
    function drawPieChart(data) {

        var svg = d3.select("svg"),
            width = svg.attr('width'),
            height = svg.attr('height'),
            radius = Math.min(width, height) / 2 - 20,
            arcX = (width / 4) + 15,
            arcY = height / 2,
            legendX = (width / 2) + 50,
            g = svg.append("g").attr("transform", "translate(" + arcX + "," + arcY + ")");

       
        // Generate the pie
        var pie = d3.pie()
            .value(function (d) { return d.value; });

        // Generate the arcs 
        var arc = d3.arc()
            .innerRadius(0)
            .outerRadius(radius);

        //Generate groups
        var arcs = g.selectAll("arc")
            .data(pie(data))
            .enter()
            .append("g")
            .attr("class", "arc");

        //Draw arc paths      
        arcs.append("path")
            .attr('stroke', function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return 'black'
                else
                    return '';
            })
            .attr("fill", function (d, i) { return getFillColor(d, i, data[i].key); })
            .attr("d", arc);            

        //Append values on chart
        arcs.append("text")
            .attr("transform", function (d) { return "translate(" + arc.centroid(d) + ")"; })
            .style("font-size", "11px")
            .style("font-family", "Arial")
            .attr("text-anchor", "middle")
            .text(function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return '0%';
                if (data[i].value > 0)
                    return data[i].value + '%';
            });

        //Generate legend
        var legend = svg.selectAll("legend")
            .data(pie(data))
            .enter()
            .append("g")
            .attr("transform", function (d, i) { return "translate(" + legendX + "," + (i * 15 + 20) + ")"; });

        //Append legend box
        legend.append("rect")
            .attr("width", 10)
            .attr("height", 10)
            .attr("fill", function (d, i) { return getFillColor(d, i, data[i].key); });

        //Append legend text
        legend.append("text")
            .text(function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return ' (0%)';
                return truncate(data[i].key) + " (" + data[i].value + "%)";
            })
            .style("font-size", "11px")
            .style("font-family", "Arial")
            .attr("x", 12)
            .attr("y", 8);
    }

    /****************************************************************************************
    *  IMPORTANT: This is a copy of drawBarChart() in CityWatch.Web\wwwroot\jsreport.js
    *  Any changes - should be done in both places
    *****************************************************************************************/
    function drawBarChart(data) {

        var margin = { top: 20, right: 30, bottom: 40, left: 100 },
            width = 460 - margin.left - margin.right,
            height = 400 - margin.top - margin.bottom;

        var svg = d3.select("svg")
            .attr('width', width + margin.left + margin.right)
            .attr('height', height + margin.top + margin.bottom)
            .append("g")
            .attr('transform', 'translate(' + margin.left + "," + margin.top + ')');

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
            .attr('text-anchor', 'end');

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
            .domain(data.map(function (d) { return d.key }))
            .padding(.1);

        svg.append('g').call(d3.axisLeft(y))

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
            .attr('width', function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return 0;
                else
                    return x(d.value);
            })
            .attr('height', y.bandwidth())
            .attr('fill', function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return '#FFFFFF';
                else
                    return '#00468b';                
            })

        // values on bar chart
        svg.selectAll('text.bar')
            .data(data)
            .enter().append("text")
            .attr("font-size", "11px")
            .attr("font-family", "Arial")
            .attr('x', function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return 0;
                else
                    return x(d.value) + 8;
            })
            .attr('y', function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return 0;
                else
                    return y(d.key) + 12;
            })
            .attr('width', function (d) { return x(d.value); })
            .attr('height', y.bandwidth())
            .text(function (d, i) {
                if (data[i].key.toLowerCase() == 'no/data')
                    return 0;
                if (data[i].value > 0)
                    return data[i].value
            });
    }

    /****************************************************************************************
    *  Vertical (column) chart for time-ordered series, e.g. SITE COMBINED WAND STRIKES.
    *  Labels are allowed to repeat (day letters M T W T F S S ...), so bars are positioned
    *  by index, NOT by label. Do not reuse drawBarChart for such series: its band scale is
    *  keyed by label, and duplicate labels collapse onto the same band.
    *  Mirrors the look of drawBarChartUsingChartJsDailyWandStrikeData in
    *  CityWatch.Web\wwwroot\js\reports.js (chronological bars, value above each bar).
    *****************************************************************************************/
    function drawColumnChart(data, chartWidth) {

        var margin = { top: 25, right: 15, bottom: 35, left: 45 },
            width = (chartWidth || 500) - margin.left - margin.right,
            height = 320 - margin.top - margin.bottom;

        var svg = d3.select("svg")
            .attr('width', width + margin.left + margin.right)
            .attr('height', height + margin.top + margin.bottom)
            .append('g')
            .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

        var noData = data.length === 1 && data[0].key.toLowerCase() === 'no/data';

        // X: band over indices so duplicate labels each keep their own bar
        var x = d3.scaleBand()
            .range([0, width])
            .domain(d3.range(data.length))
            .padding(0.15);

        var maxValue = noData ? 0 : d3.max(data, function (d) { return d.value; });

        var y = d3.scaleLinear()
            .domain([0, maxValue > 0 ? maxValue : 1])
            .nice()
            .range([height, 0]);

        // The PNG is downscaled hard in the PDF (320px tall -> ~150pt), so these sizes are
        // deliberately larger than they would be for an on-screen chart. "Dense" is judged
        // by the actual bar width, not the bar count: 31 bars on a full-page chart still
        // have room for straight 12px labels.
        var dense = x.bandwidth() < 20;
        var labelFontSize = dense ? '10px' : '12px';

        svg.append('g')
            .attr('transform', 'translate(0,' + height + ')')
            .call(d3.axisBottom(x).tickFormat(function (i) { return noData ? '' : data[i].key; }))
            .selectAll('text')
            .attr('font-size', labelFontSize)
            .attr('font-family', 'Arial');

        svg.append('g')
            .call(d3.axisLeft(y).ticks(5))
            .selectAll('text')
            .attr('font-size', '12px')
            .attr('font-family', 'Arial');

        if (noData) {
            svg.append('text')
                .attr('x', width / 2)
                .attr('y', height / 2)
                .attr('text-anchor', 'middle')
                .attr('font-size', '12px')
                .attr('font-family', 'Arial')
                .text('No Data');
            return;
        }

        // bars
        svg.selectAll('columnbar')
            .data(data)
            .enter()
            .append('rect')
            .attr('x', function (d, i) { return x(i); })
            .attr('y', function (d) { return y(d.value); })
            .attr('width', x.bandwidth())
            .attr('height', function (d) { return height - y(d.value); })
            .attr('fill', '#4682b4');

        // value above each bar (hidden for zero so the axis stays clean).
        // On dense charts every other label is raised a little so neighbouring
        // values don't overlap each other.
        svg.selectAll('text.columnbar')
            .data(data)
            .enter().append('text')
            .attr('font-size', dense ? '10px' : '12px')
            .attr('font-family', 'Arial')
            .attr('text-anchor', 'middle')
            .attr('x', function (d, i) { return x(i) + x.bandwidth() / 2; })
            .attr('y', function (d, i) { return y(d.value) - 3 - (dense && i % 2 === 1 ? 10 : 0); })
            .text(function (d) { return d.value > 0 ? d.value : ''; });
    }
}

module.exports = { drawChart };