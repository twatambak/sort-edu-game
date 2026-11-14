mergeInto(LibraryManager.library, {
  DownloadCsvFile: function (pathPointer) {
    try {
      var path = UTF8ToString(pathPointer);
      var bytes = FS.readFile(path);
      var blob = new Blob([bytes], { type: "text/csv;charset=utf-8" });
      var url = URL.createObjectURL(blob);
      var link = document.createElement("a");
      link.href = url;
      link.download = "ondevai-dados.csv";
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
      return 1;
    } catch (error) {
      console.error("Could not download the CSV export.", error);
      return 0;
    }
  }
});
