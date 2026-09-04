const d3 = require("d3");
const jsdom = require("jsdom");
const { convert } = require('convert-svg-to-png');
const fs = require('fs');

module.exports = function (callback, options, data) {

    const { JSDOM } = jsdom;
    const { document } = (new JSDOM('')).window;
    global.document = document;
    var body = d3.select(document).select("body");

    var width = 300,
        height = 150,
        margin = { top: 10, right: 40, bottom: 30, left: 40 },
        barPadding = 0.2,
        axisTicks = { qty: 5, outerSize: 0 },
        color_flir = "#4472c4", color_wand = "#ee9a00";;


    var svg = body.append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    var xScale0 = d3.scaleBand().range([0, width - margin.left - margin.right]).padding(barPadding);
    var xScale1 = d3.scaleBand();
    var yScale = d3.scaleLinear().range([height - margin.top - margin.bottom, 0]);
    var yScaleRight = d3.scaleLinear().range([height - margin.top - margin.bottom, 0]);

    var xAxis = d3.axisBottom(xScale0).tickSizeOuter(axisTicks.outerSize);
    var yAxis = d3.axisLeft(yScale).ticks(axisTicks.qty).tickSizeOuter(axisTicks.outerSize);
    var yAxisRight = d3.axisRight(yScaleRight).ticks(axisTicks.qty).tickSizeOuter(axisTicks.outerSize);

    xScale0.domain(data.map(d => d.weekNumber));
    xScale1.domain(['flir', 'wand']).range([0, xScale0.bandwidth()]);
    yScale.domain([0, d3.max(data, d => d.flir) + 1]);
    yScaleRight.domain([0, d3.max(data, d => d.wand) + 1]);

    var weekNumber = svg.selectAll(".weekNumber")
        .data(data)
        .enter().append("g")
        .attr("class", "weekNumber")
        .attr("transform", d => `translate(${xScale0(d.weekNumber)},0)`);

    /* Add flir bars */
    weekNumber.selectAll(".bar.flir")
        .data(d => [d])
        .enter()
        .append("rect")
        .attr("class", "bar flir")
        .style("fill", "#4472c4")
        .attr("x", d => xScale1('flir'))
        .attr("y", d => yScale(d.flir))
        .attr("width", xScale1.bandwidth())
        .attr("height", d => {
            return height - margin.top - margin.bottom - yScale(d.flir)
        });

    /* Add wand bars */
    weekNumber.selectAll(".bar.wand")
        .data(d => [d])
        .enter()
        .append("rect")
        .attr("class", "bar wand")
        .style("fill", "#ee9a00")
        .attr("x", d => xScale1('wand'))
        .attr("y", d => yScaleRight(d.wand))
        .attr("width", xScale1.bandwidth())
        .attr("height", d => {
            return height - margin.top - margin.bottom - yScaleRight(d.wand)
        });

    // Add the X Axis
    svg.append("g")
        .attr("class", "x axis")
        .attr("transform", `translate(0,${height - margin.top - margin.bottom})`)
        .call(xAxis);

    // Add the Y Axis
    svg.append("g")
        .attr("class", "y axis")
        .call(yAxis);

    const legend = svg.append('g')
        .attr("class", "legend")
        .attr('transform', `translate(${margin.left + 10}, ${height - margin.top - 5})`) // w.r.t Y-Axis;

    // Add legend - FLIR
    legend.append('rect')
        .attr('x', 0)
        .attr('y', -10)
        .attr('width', 10)
        .attr('height', 10)
        .attr('fill', color_flir);

    legend.append('text')
        .attr('x', 15)
        .attr('y', 0)
        .text('FLIR')
        .style("font-size", "9px");

    // Add legend - WAND
    legend.append('rect')
        .attr('x', 60)
        .attr('y', -10)
        .attr('width', 10)
        .attr('height', 10)
        .attr('fill', color_wand);

    legend.append('text')
        .attr('x', 75)
        .attr('y', 0)
        .text('WAND')
        .style("font-size", "9px");

    // Add the Y Axis right
    // 220 = x-axis
    svg.append("g")
        .attr("class", "y axis")
        .attr("transform", `translate(220 ,${margin.right, margin.top - 10})`)
        .call(yAxisRight);

    convert(body.node().innerHTML)
        .then(buffer => fs.writeFile(options.fileName, buffer, () => callback(null, "OK")))
        .catch(e => console.error(e));
}