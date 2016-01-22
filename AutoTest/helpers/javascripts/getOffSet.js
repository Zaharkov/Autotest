document.getElementByXPath = (function (sValue) {
    var a = this.evaluate(sValue, this, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
    if (a.snapshotLength > 0) {
        return a.snapshotItem(0);
    }

    return null;
});

document.getOffset = (function (elem) {
    if (elem.getBoundingClientRect) {
        // "правильный" вариант
        return document.getOffsetRect(elem);
    } else {
        // пусть работает хоть как-то
        return document.getOffsetSum(elem);
    }
});

document.getOffsetSum = (function (elem) {
    var top = 0, left = 0;
    while (elem) {
        top = top + parseInt(elem.offsetTop);
        left = left + parseInt(elem.offsetLeft);
        elem = elem.offsetParent;
    }

	return { top: top, left: left };
});

document.getOffsetRect = (function (elem) {
    // (1)
    var box = elem.getBoundingClientRect();

    // (2)
    var body = document.body;
    var docElem = document.documentElement;

    // (3)
    var scrollTop = window.pageYOffset || docElem.scrollTop || body.scrollTop;
    var scrollLeft = window.pageXOffset || docElem.scrollLeft || body.scrollLeft;

    // (4)
    var clientTop = docElem.clientTop || body.clientTop || 0;
    var clientLeft = docElem.clientLeft || body.clientLeft || 0;

    // (5)
    var top = box.top + scrollTop - clientTop;
    var left = box.left + scrollLeft - clientLeft;
    var height = box.height;
    var width = box.width;

	return { top: Math.round(top), left: Math.round(left), height: Math.round(height), width: Math.round(width) };
});