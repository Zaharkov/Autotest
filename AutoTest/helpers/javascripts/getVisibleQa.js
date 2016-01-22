mainModule.directive('qa', function () {
    return function ($scope, $element) {
        $scope.$watch(function () {

            var text = '';
            var el = $element[0];
            var attr = el.getAttribute("data-qa");

            if ($element.qaValue !== undefined) {
                text = $element.qaValue;
            } else if (attr !== null)
                text = attr;

            $element.qaValue = text;
            var result = getVisible(el);

            if (!result)
                el.removeAttribute("data-qa");
            else {
                if (attr !== text)
                    el.setAttribute("data-qa", text);
            }
        });

        function getVisible(el) {
            var visible = true;

            while (el != null) {
                var display = window.getComputedStyle(el)['display'];

                if (display.indexOf("none") > -1)
                    visible = false;

                el = el.parentElement;
            }

            return visible;
        }
    };
});


mainModule.run(function ($rootScope) {

    function isVisible (el) {
        var display = window.getComputedStyle(el)['display'];

        if (display.indexOf('none') > -1) {
            return false;
        } else {
            return true;
        }
    }

    function recursion (element, visibleRoot) {

        if (element.nodeType === 1) {

            var attr = element.getAttribute('data-qa');
            var text = element.qaValue ? element.qaValue : attr;
            var result = visibleRoot ? isVisible(element) : visibleRoot;

            if (text) {

                if (!element.qaValue) {
                    element.qaValue = text;
                }

                if (!result) {
                    element.removeAttribute('data-qa');
                } else {
                    if (attr !== text) {
                        element.setAttribute('data-qa', text);
                    }
                }
            }

            _.each(element.childNodes, function (child) {
                recursion(child, result);
            });
        }
    }

    function calcVisibleQa () {
        recursion(document.body, true);
    }

    $rootScope.$watch(function () {

        var isRun = true;

        _.each(window._activeSetTimeouts, function (time) {
            if (time['cb'].indexOf('calcVisibleQa') > -1) {
                isRun = false;
            }
        });

        if (isRun) {
            setTimeout(calcVisibleQa, 0);
        }
    });
});
