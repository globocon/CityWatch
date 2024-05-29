const d3 = require("d3");
const jsdom = require("jsdom");
const { convert } = require('convert-svg-to-png');
const fs = require('fs');

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

    body.append("svg")
        .attr("width", options.width | 500)
        .attr("height", 320);

    // 1 = Pie chart 2 = Bar chart
    switch (options.type) {
        case 1:
            drawPieChart(data);
            break;
        case 2:
            drawBarChart(data)
            break;
    }

    convert(body.node().innerHTML)
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
            .attr('width', function (d) { return x(d.value); })
            .attr('height', y.bandwidth())
            .attr('fill', '#00468b')

        // values on bar chart
        svg.selectAll('text.bar')
            .data(data)
            .enter().append("text")
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
}

module.exports = { drawChart };