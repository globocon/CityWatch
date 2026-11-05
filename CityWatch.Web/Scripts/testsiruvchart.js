/*
 * 
 * Steps to run a test:
 * ====================
 * 1. In terminal window, navigate to this folder - for e.g. "C:\citywatch\src\CityWatch.Web\Scripts\"
 * 2. Uncomment test function to execute - for e.g. create_ir_bar_chart()
 * 3. Run command "node .\testsiruvchart.js" from terminal
 * 
 */

const iruvChart = require("./ir-uvchart.js");

create_ir_uv_chart();

/****** Test functions ******/
function create_ir_uv_chart() {
    var date = new Date();
    var options = {
        fileName: '../wwwroot/GraphImage/uvChart' + date.getHours() + '' + date.getMinutes() + '.png',
        width: 700
    };
    //var test_data = [{ key: 'abc', value: 10 }, { key: 'pqr', value: 3 },]
    //var callback = function () { console.log('Created file' + options.fileName); }

   // irChart.drawChart(callback, options, test_data);


   iruvChart.drawUvChart(
        (err, result) => console.log(result),
        options,
        ["06:00", "07:00", "08:00", "09:00", "10:00", "11:00", "12:00"],
        [0, 0.5, 2, 4, 7, 9, 8]
    );

}

