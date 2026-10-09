
let rerailfolder_Name = '';
let rerailworkOrderid = '';

const rerailexcelButtons = document.querySelectorAll('[data-rerailscopeexcelfolder]');

if (rerailexcelButtons.length > 0) {
    rerailexcelButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            rerailfolder_Name = this.getAttribute('data-rerailscopeexcelfolder');
            rerailworkOrderid = this.getAttribute('data-rerailscopeexcelworkorder');

            const formData = new FormData();
            formData.append("formName", rerailfolder_Name); // Add folder Name info to request
            formData.append("workOrder", rerailworkOrderid); // Add supervisor Name info to request
            const token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/api/retailscopingwebhook/exceldatadownload',
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
                    a.download = `${rerailfolder_Name}_${rerailworkOrderid}_Output_data.xlsx`;
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