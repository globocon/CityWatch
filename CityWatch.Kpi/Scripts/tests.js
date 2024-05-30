/*
 * 
 * Steps to run a test:
 * ====================
 * 1. In terminal window, navigate to this folder - for e.g. "C:\citywatch\src\CityWatch.Kpi\Scripts\"
 * 2. Uncomment test function to execute - for e.g. create_ir_bar_chart()
 * 3. Run command "node .\tests.js" from terminal
 * 
 */

const irChart = require("./ir-chart.js");

//create_ir_bar_chart();
create_ir_pie_chart();

/****** Test functions ******/
function create_ir_bar_chart() {
    var date = new Date();
    var options = {
        fileName: '../wwwroot/GraphImage/' + date.getHours() + '' + date.getMinutes() + '.png',
        type: 2 // bar chart
    };
    var test_data = [{ key: 'abc', value: 10 }, { key: 'pqr', value: 3 },]
    var callback = function () { console.log('Created file' + options.fileName); }

    irChart.drawChart(callback, options, test_data);
}

function create_ir_pie_chart() {
    var date = new Date();
    var options = {
        fileName: '../wwwroot/GraphImage/' + date.getHours() + '' + date.getMinutes() + '.png',
        type: 1, // pie chart
        width: 600
    };
    var test_data = [{ key: 'abc', value: 10 }, { key: 'pqr', value: 3 }, { key: 'no/data', value: 100 },]
    var callback = function () { console.log('Created file' + options.fileName); }

    irChart.drawChart(callback, options, test_data);
}

