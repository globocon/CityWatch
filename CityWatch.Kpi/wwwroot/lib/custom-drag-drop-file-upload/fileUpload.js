

var allowedfiletypes = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
var allowedfiletypestext = allowedfiletypes.join(", ");
var fileuploadinfotable = null;
var fileuploadinfotableBody = null;

var fileprocess = null;

(function ($) {
    var fileUploadCount = 0;
    
    $.fn.fileUpload = function () {
        return this.each(function () {
            var fileUploadDiv = $(this);
            var fileUploadId = "upload_KeyImage_file"; //  `fileUpload-${++fileUploadCount}`;

            // Creates HTML content for the file upload area.
            var fileDivContent = `
                <label for="${fileUploadId}" class="file-upload">
                    <div class="m-2 p-1">
                         <div class="border-0">
                            <i class="fa fa-cloud-upload fa-lg text-primary"></i>
                            <small style="line-height: 5px; font-size:11px;">
                            Drag and drop files here or click here to select a file with extension ${allowedfiletypestext}
                            <small>
                         </div>
                     </div>                
                    <input type="file" id="${fileUploadId}" accept="${allowedfiletypestext}" name=[] hidden />
                </label>
            `;

            fileUploadDiv.html(fileDivContent).addClass("file-container");

           
            // Creates a table containing file information.
            function createTable() {
                fileuploadinfotable = $(`
                <label class="mt-1 font-bold">Key Image</label>
                    <table>
                        <thead>
                            <tr>
                                <th></th>
                                <th style="width: 30%;">File Name</th>
                                <th>Preview</th>
                                <th style="width: 20%;">Size</th>
                                <th>Type</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                        </tbody>
                    </table>
                `);

                fileuploadinfotableBody = fileuploadinfotable.find("tbody");
                fileUploadDiv.append(fileuploadinfotable);
            }

            if (!fileuploadinfotable) {
               /* createTable();*/
            }

            // Adds the information of uploaded files to table.
            fileprocess = function (files) { //handleFiles
                if (!fileuploadinfotable) {
                   /* createTable();*/
                }

                /*fileuploadinfotableBody.empty();*/
                if (files.length > 0) {
                    $.each(files, function (index, file) {
                        /*var fileNameModified = */                        
                        var ext = "." + file.name.split('.').pop().toLowerCase();
                        //console.log('ext: ' + ext);
                        if (allowedfiletypes.includes(ext)) {
                            //var fileName = file.name;
                            //var fileType = file.type;
                            //var fileSize = (file.size / 1024).toFixed(2) + " KB";
                            //var preview = fileType.startsWith("image")
                            //    ? `<img src="${URL.createObjectURL(file)}" alt="${fileName}" height="30">`
                            //    : `<i class="material-icons-outlined">visibility_off</i>`;

                            //fileuploadinfotableBody.append(`
                            //<tr>
                            //    <td>${index + 1}</td>
                            //    <td><span id="guardComplianceandlicense_fileName1"></span></td>
                            //    <td>${preview}</td>
                            //    <td>${fileSize}</td>
                            //    <td>${fileType}</td>
                            //    <td><button type="button" class="deleteBtn"><i class="material-icons-outlined">delete</i></button></td>
                            //</tr>
                        /*`);*/
                        }
                        else {
                            /*alert('Invalid file type !!!');*/
                        }
                        
                    });

                    //fileuploadinfotableBody.find(".deleteBtn").click(function () {
                    //    $(this).closest("tr").remove();

                    //    if (fileuploadinfotableBody.find("tr").length === 0) {
                    //        fileuploadinfotableBody.append('<tr><td colspan="6" class="no-file">No files selected!</td></tr>');
                    //    }
                    //});
                }
            }

            // Events triggered after dragging files.
            fileUploadDiv.on({
                dragover: function (e) {
                    e.preventDefault();
                    fileUploadDiv.toggleClass("dragover", e.type === "dragover");
                },
                drop: function (e) {
                    e.preventDefault();
                    fileUploadDiv.removeClass("dragover");
                    //handleFiles(e.originalEvent.dataTransfer.files);
                    //fileprocess(e.originalEvent.dataTransfer.files);
                    FileuploadFileChanged(e.originalEvent.dataTransfer.files);
                },
            });

            // Event triggered when file is selected.
            fileUploadDiv.find(`#${fileUploadId}`).change(function () {
                //handleFiles(this.files);
               // fileprocess(this.files);
            });
        });
    };

    $(document).on('dragenter', function (e) {
        e.stopPropagation();
        e.preventDefault();
    });
    $(document).on('dragover', function (e) {
        e.stopPropagation();
        e.preventDefault();
        //obj.css('border', '2px dotted #0B85A1');
    });
    $(document).on('drop', function (e) {
        e.stopPropagation();
        e.preventDefault();
    });
})(jQuery);
