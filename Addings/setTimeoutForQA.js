if (typeof window._setTimeout == 'undefined') {
    window._setTimeout = window.setTimeout;

    window._activeSetTimeouts = {};
    window._activeSetTimeoutsTotal = 0;
    window._setTimeoutCounter = 0;
    window.setTimeout = function(cb, delay) {
        var id = window._setTimeoutCounter++;
        var handleId = window._setTimeout(function() {
            window._activeSetTimeouts[id].status = 'exec';
            cb();
            delete window._activeSetTimeouts[id];
            window._activeSetTimeoutsTotal--;
        }, delay);

        window._activeSetTimeouts[id] = {
            calltime: new Date().getTime().toString(),
            delay: delay,
            cb: cb.toString(),
            status: 'wait'
        };
        window._activeSetTimeoutsTotal++;
        return handleId;
    };
}

if (typeof window._setInterval == 'undefined') {
    window._setInterval = window.setInterval;

    window._activeSetIntervals = {};
    window._activeSetIntervalsTotal = 0;
    window._setIntervalCounter = 0;
    window.setInterval = function (cb, delay) {
        var id = window._setIntervalCounter++;
        var handleId = window._setInterval(function () {
            window._activeSetIntervals[id].status = 'exec';
            cb();
        }, delay);

        window._activeSetIntervals[id] = {
            calltime: new Date().getTime().toString(),
            delay: delay,
            cb: cb.toString(),
            status: 'wait'
        };
        window._activeSetIntervalsTotal++;
        return handleId;
    };
}