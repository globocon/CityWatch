function drawUvChart(callback, options, timeData, uvData) {

    const { JSDOM } = require("jsdom");
    const d3 = require("d3");
    const { convert } = require("convert-svg-to-png");
    const fs = require("fs");

    const dom = new JSDOM('<!DOCTYPE html><body></body>');
    const document = dom.window.document;
    global.document = document;

    const width = options.width || 800;
    const height = options.height || 450;

    const margin = { top: 20, right: 120, bottom: 60, left: 80 };
    const chartWidth = width - margin.left - margin.right;
    const chartHeight = height - margin.top - margin.bottom;

    const body = d3.select(document).select("body");

    const svg = body.append("svg")
        .attr("width", width)
        .attr("height", height)
        .style("background", "#ffffff");

    const g = svg.append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // ========================
    // SCALES
    // ========================

    const x = d3.scalePoint()
        .domain(timeData)
        .range([0, chartWidth]);

    const maxUV = 16; // ARPANSA fixed scale

    const y = d3.scaleLinear()
        .domain([0, maxUV])
        .range([chartHeight, 0]);

    // ========================
    // UV ZONES (ARPANSA COLORS)
    // ========================

    const zones = [
        { from: 0, to: 2.5, color: "#d4f2bc", label: "Low" },
        { from: 2.5, to: 5.5, color: "#f9f5c2", label: "Moderate" },
        { from: 5.5, to: 7.5, color: "#fde1bf", label: "High" },
        { from: 7.5, to: 10.5, color: "#f2b8bf", label: "Very High" },
        { from: 10.5, to: 16, color: "#e0dbf9", label: "Extreme" }
    ];

    zones.forEach(zone => {

        // Background band
        g.append("rect")
            .attr("x", 0)
            .attr("width", chartWidth)
            .attr("y", y(zone.to))
            .attr("height", y(zone.from) - y(zone.to))
            .attr("fill", zone.color)
            .attr("opacity", 0.7);

        // Right-side zone label
        g.append("text")
            .attr("x", chartWidth - 75)
            .attr("y", y((zone.from + zone.to) / 2))
            .attr("alignment-baseline", "middle")
            .style("font-family", "Arial")
            .style("font-size", "13px")
            .style("font-weight", "bold")
            .text(zone.label);
    });


    // ========================
    // AXES
    // ========================

    const xAxis = d3.axisBottom(x);
    const yAxis = d3.axisLeft(y)
        .ticks(8);

    g.append("g")
        .attr("transform", `translate(0,${chartHeight})`)
        .call(xAxis)
        .selectAll("text")
        .style("font-size", "12px")
        .style("font-family", "Arial");

    g.append("g")
        .call(yAxis)
        .selectAll("text")
        .style("font-size", "12px")
        .style("font-family", "Arial");

    // Remove axis lines for cleaner ARPANSA look
    g.selectAll(".domain").remove();

    // Axis Titles
    svg.append("text")
        .attr("x", width / 2)
        .attr("y", height - 15)
        .attr("text-anchor", "middle")
        .style("font-family", "Arial")
        .style("font-size", "14px")
        .style("font-weight", "bold")
        .text("Time of day");

    svg.append("text")
        .attr("transform", "rotate(-90)")
        .attr("x", -height / 2)
        .attr("y", 25)
        .attr("text-anchor", "middle")
        .style("font-family", "Arial")
        .style("font-size", "14px")
        .style("font-weight", "bold")
        .text("Ultraviolet radiation level");

    // ========================
    // UV LINE (Measured)
    // ========================

    const line = d3.line()
        .x((d, i) => x(timeData[i]))
        .y(d => y(d))
        .curve(d3.curveMonotoneX);

    g.append("path")
        .datum(uvData)
        .attr("fill", "none")
        .attr("stroke", "#00AEEF")
        .attr("stroke-width", 3)
        .attr("d", line);

    // Optional point markers
    g.selectAll("circle")
        .data(uvData)
        .enter()
        .append("circle")
        .attr("cx", (d, i) => x(timeData[i]))
        .attr("cy", d => y(d))
        .attr("r", 3)
        .attr("fill", "#00AEEF");

    // ========================
    // SAVE PNG
    // ========================

    convert(body.node().innerHTML)
        .then(buffer =>
            fs.writeFile(options.fileName, buffer, () => callback(null, "OK"))
        )
        .catch(e => callback(e));
}

module.exports = { drawUvChart };