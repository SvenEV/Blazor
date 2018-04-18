Blazor.registerFunction('getSize', (elementId) => {
    var elem = document.getElementById(elementId);
    var bounds = elem.getBoundingClientRect();
    return bounds.width + "," + bounds.height;
});

Blazor.registerFunction('measureHtml', (data) => {
    var container = document.getElementById('xamzorMeasureContainer');

    if (container === null) {
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
        const assemblyName = 'Xamzor';
        const namespace = 'Xamzor.UI';
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

Blazor.registerFunction('onViewRegistered', viewId => {
    var observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => {

            // find closest ancestor that is a Xamzor component
            var parent = mutation.target;
            while (parent && !(parent instanceof Element && parent.hasAttribute('xamzorid')))
                parent = parent.parentElement;
            
            mutation.addedNodes.forEach(child => {
                // Register component
                if (parent && child instanceof Element && child.hasAttribute('xamzorid')) {
                    xamzorInvokeCSharpMethod(
                        'Xamzor.UI', 'Application', 'JSRegisterElement',
                        [viewId, parent.getAttribute('xamzorid'), child.getAttribute('xamzorid')]);
                }
            });
        });
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    window.Xamzor.ViewObservers[viewId] = observer;
});

Blazor.registerFunction('onViewUnregistered', viewId => {
    var observer = window.Xamzor.ViewObservers[viewId];
    if (observer) {
        observer.disconnect();
        delete window.Xamzor.ViewObservers[viewId];
    }
})

function xamzorInvokeCSharpMethod(namespace, typeName, methodName, args) {
    const assemblyName = 'Xamzor';
    const method = Blazor.platform.findMethod(assemblyName, namespace, typeName, methodName);
    var csArgs = [];
    if (args)
        args.forEach(arg => csArgs.push(Blazor.platform.toDotNetString(arg)));
    let resultAsDotNetString = Blazor.platform.callMethod(method, null, csArgs);
}

window.Xamzor = {
    ViewObservers: {}
};

window.addEventListener('resize', () =>
    xamzorInvokeCSharpMethod('Xamzor.UI', 'Application', 'JSNotifyWindowResized'));