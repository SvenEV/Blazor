Blazor.registerFunction('getSize', (elementId) => {
    var elem = document.getElementById(elementId);
    var bounds = elem.getBoundingClientRect();
    return bounds.width + "," + bounds.height;
});

Blazor.registerFunction('measureHtml', (data) => {
    var container = document.getElementById('xamzorMeasureContainer');

    if (container == null) {
        container = document.createElement('div');
        container.style = 'display: inline-block; visibility: hidden;';
        container.id = 'xamzorMeasureContainer';
        document.body.appendChild(container);
    }

    container.innerHTML = data;
    var bounds = container.getBoundingClientRect();
    var result = bounds.width + "," + bounds.height;
    container.innerHTML = "";
    return result;
});

Blazor.registerFunction('measureImage', (source) => {
    var img = document.createElement('img');
    img.style = "visibility: collapse";
    img.src = source;

    img.onload = function () {
        returnResult();
        document.body.removeChild(img);
    };

    img.onerror = function () {
        returnResult();
        document.body.removeChild(img);
    };

    document.body.appendChild(img);

    function returnResult() {
        const assemblyName = 'StandaloneApp';
        const namespace = 'StandaloneApp.UI';
        const typeName = 'ImageMeasureInterop';
        const methodName = 'NotifyImageMeasured';

        const method = Blazor.platform.findMethod(
            assemblyName,
            namespace,
            typeName,
            methodName
        );

        let arg1AsDotNetString = Blazor.platform.toDotNetString(source);
        let arg2AsDotNetString = Blazor.platform.toDotNetString(img.naturalWidth + ',' + img.naturalHeight);

        let resultAsDotNetString = Blazor.platform.callMethod(method, null, [
            arg1AsDotNetString,
            arg2AsDotNetString
        ]);
    }
});