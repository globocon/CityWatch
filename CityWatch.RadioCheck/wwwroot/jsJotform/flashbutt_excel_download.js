
let folder_Name = '';
let supervisor_Name = '';

const excelButtons = document.querySelectorAll('[data-excelfiledate]');

if (excelButtons.length > 0) {
    excelButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            folder_Name = this.getAttribute('data-excelfiledate');
            supervisor_Name = this.getAttribute('data-excelfilesupervisor');

            const formData = new FormData();
            formData.append("folder_Name", folder_Name); // Add folder Name info to request
            formData.append("supervisor_Name", supervisor_Name); // Add supervisor Name info to request
            const token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/api/flashbuttwelding/exceldatadownload',
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                xhrFields: {
                    responseType: 'blob' // This is crucial to receive binary data
                },
                headers: { 'RequestVerificationToken': token },
                success: function (blob, status, xhr) {
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = `${folder_Name}_${supervisor_Name}_LWRReport.xlsx`;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    window.URL.revokeObjectURL(url);
                },
                error: function (xhr, status, error) {
                    console.error("Download failed:", error);
                    $.notify("Download failed",
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
            });

        });
    });
}