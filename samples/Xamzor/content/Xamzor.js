Blazor.registerFunction('Xamzor.getParentComponent', (elementId) => {
    var elem = document.getElementById(elementId);

    if (!elem)
        return null;

    var parent = elem.parentElement;
    while (parent && !(parent instanceof Element && parent.hasAttribute('xamzorid')))
        parent = parent.parentElement;

    return parent ? parent.getAttribute('xamzorid') : null;
});

function xamzorInvokeCSharpMethod(namespace, typeName, methodName, args) {
    const assemblyName = 'Xamzor';
    const method = Blazor.platform.findMethod(assemblyName, namespace, typeName, methodName);
    var csArgs = [];
    if (args)
        args.forEach(arg => csArgs.push(Blazor.platform.toDotNetString(arg)));
    let resultAsDotNetString = Blazor.platform.callMethod(method, null, csArgs);
}

window.addEventListener('resize', () =>
    xamzorInvokeCSharpMethod('Xamzor.UI', 'Application', 'JSNotifyWindowResized'));