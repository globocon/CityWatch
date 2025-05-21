
let fileType = '';
let form_Name = '';

//document.getElementById('uploadButton').addEventListener('click', function () {
//    fileType = this.getAttribute('data-exceluploadfiletype');
//    document.getElementById('excelFile').click();
//});

document.querySelectorAll('[data-exceluploadfiletype]').forEach(btn => {
    btn.addEventListener('click', function () {
        fileType = this.getAttribute('data-exceluploadfiletype');
        form_Name = this.getAttribute('data-exceluploadformName');
        document.getElementById('excelFile').click();
    });
});

document.getElementById('excelFile').addEventListener('change', function (event) {
    const file = event.target.files[0];

    if (!file) {
        alert("No file selected.");
        return;
    }

    const validTypes = [
        'application/vnd.ms-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    ];

    if (!validTypes.includes(file.type)) {
        alert("Please select a valid Excel file (.xls or .xlsx).");
        return;
    }

    const formData = new FormData();
    formData.append("file", file);
    formData.append("fileType", fileType); // Add file type info to request

    $.ajax({
        url: '/api/webhook/exceldatafileupload',
        type: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        headers: { 'RequestVerificationToken': token },
        success: function (response) {
            console.log("Upload successful. Parsed data:", response);            
            if (response.status) {
                //Success
                $.notify(response.message,
                    {
                        align: "center",
                        verticalAlign: "top",
                        color: "#fff",
                        background: "#20D67B",
                        blur: 0.4,
                        delay: 0
                    }
                );
            } else {
                //Failed
                $.notify(response.message,
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
        },
        error: function (xhr, status, error) {
            console.error("Upload failed:", error);
            alert("Upload failed.");
        }
    });
});