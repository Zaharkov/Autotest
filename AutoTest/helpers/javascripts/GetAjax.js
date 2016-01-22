var newArray = new Object();
newArray['SetTimeouts'] = new Object();
newArray['SetIntervals'] = new Object();
newArray['Ajax'] = 0;

var setTimeouts = window._activeSetTimeouts;
for (var keyTimeouts in setTimeouts) {
    if (setTimeouts.hasOwnProperty(keyTimeouts)) {
        if (keyTimeouts > arguments[0] && setTimeouts[keyTimeouts]['delay'] < 5000) {
            newArray['SetTimeouts'][keyTimeouts] = setTimeouts[keyTimeouts];
        }
    }
}

var setIntervals = window._activeSetIntervals;
for (var keyIntervals in setIntervals) {
    if (setIntervals.hasOwnProperty(keyIntervals)) {
        if (keyIntervals > arguments[1] && setIntervals[keyIntervals]['delay'] < 5000) {
            newArray['SetIntervals'][keyIntervals] = setIntervals[keyIntervals];
        }
    }
}

var ajaxSum = 0;
if (document.readyState !== 'complete')
    ajaxSum++;

if (typeof window.activeRequests != 'undefined')
    ajaxSum += window.activeRequests;

if (typeof window.jQuery != 'undefined')
    ajaxSum += window.jQuery.active;

if (typeof window.animationCount != 'undefined')
    ajaxSum += window.animationCount;

newArray['Ajax'] = ajaxSum;
newArray['TimeNow'] = new Date().getTime();
return newArray;