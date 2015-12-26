(function (angular, $, _) {
    angular
        .module('mainModule')
        .directive('visibleDataQa', visibleDataQa);

    visibleDataQa.$inject = ['$rootScope', '$window'];

    function visibleDataQa ($rootScope, $window) {
        return {
            restrict: 'A',
            link: visibleFunc
        };

        function visibleFunc () {

            document.body.addEventListener("DOMSubtreeModified", function () {

                if (!$window.isApplyRun) {
                    $window.isApplyRun = true;

                    _.defer(function () {
                        $rootScope.$apply();
                        $window.isApplyRun = false;
                    });
                }
            }, false);

            function isVisible (el) {
                var style = $window.getComputedStyle(el);

                return style.display !== 'none' && style.visibility !== 'hidden';
            }

            function setDataQaAttrByVisible (element, visibleRoot) {

                if (Number(element.nodeType) === 1) {

                    var text = element.qaAttrs
                        ? element.qaAttrs.qa
                        : element.qaValue || element.getAttribute('data-qa');

                    var result = visibleRoot && isVisible(element);

                    if (text) {

                        if (!element.qaValue)
                            element.qaValue = text;

                        if (result) {
                            element.setAttribute('data-qa', text);
                        } else {
                            element.removeAttribute('data-qa');
                        }
                    }

                    _.each(element.childNodes, function (child) {
                        setDataQaAttrByVisible(child, result);
                    });
                }
            }

            function calcVisibleQa () {
                setDataQaAttrByVisible(document.body, true);
                $window.isCalcVisibleQaRun = false;
            }

            $rootScope.$watch(function () {
                if (!$window.isCalcVisibleQaRun) {
                    $window.isCalcVisibleQaRun = true;
                    setTimeout(calcVisibleQa, 0);
                }
            });
        }
    }

    angular
        .module('mainModule')
        .directive('qa', dataQa);
    
    function dataQa () {
        return {
            restrict: 'A',
            link: qaFunc
        };

        function qaFunc (scope, element, attrs) {
            element[0].qaAttrs = attrs;
        }
    }
})(angular, $, _);
