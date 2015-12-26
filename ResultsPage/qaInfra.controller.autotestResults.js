(function () {
    'use sctrict';

    angular
        .module('qaInfra')
        .controller('autotestResultsCtrl', autotestResults)
        .directive('draggable', function ($document) {
            return function(scope, element) {
                var startX = 0, startY = 0, x = 0, y = 0;
                element.bind('mousedown', function(event) {
                    // Prevent default dragging of selected content
                    event.preventDefault();
                    startX = event.screenX - x;
                    startY = event.screenY - y;
                    $document.bind('mousemove', mousemove);
                    $document.bind('mouseup', mouseup);
                });

                function mousemove(event) {
                    y = event.screenY - startY;
                    x = event.screenX - startX;
                    element.css({
                        top: y + 'px',
                        left: x + 'px'
                    });
                }

                function mouseup() {
                    $document.unbind('mousemove', mousemove);
                    $document.unbind('mouseup', mouseup);
                }

            };
        });

    autotestResults.$inject = ['$http', '$filter', '$interval', 'layers', '$compile'];

    function autotestResults($http, $filter, $interval, layers, $compile) {
        var cs = this;

        //для спинера загрузки страницы
        cs.isload = false;
        cs.mainSpinner = true;

        //для отображения загрузки прогона
        cs.preloadGet = false;
        cs.caseSelected = false;

        //выгружаем модель из сервера
        //все преобразования сделал на бек-енде чтобы получить уже готовую модель
        cs.loadResult = function () {
            cs.preloadGet = true;
            cs.caseSelected = false;
            cs.ShowErrorType = 'NotChecked';
            $http.post('Autotest/GetParallel2', {'parallelId': cs.result.Id}).success(function (response) {

                cs.Parallel = response;

                //вспомогательный объект для дропдауна смены пути к скринам
                cs.Parallel.NewScreenPath = {
                    Value: '',
                    Valid: true,
                    isFirstClick: true,
                    invalidText: 'некорректный путь',
                    preloadSave: false
                };

                cs.FilterData = {
                    All: 0,
                    Checked: 0,
                    NotChecked: 0,
                    OwnerName: "Все"
                };

                cs.ownersList = response.Owners;

                if (cs.ownersList.length > 0) {
                    cs.tabValue = cs.ownersList[0].OwnerName;
                    //для динамического обновления табов
                    cs.updateValue = true;
                    //showErrorsInFirstLoad();
                    cs.showTests(cs.ownersList[0]);

                    var allOwners = { OwnerName: "Все", IsChecked: true, Tests: [] };
                    cs.ownersList.forEach(function(owner) {
                        allOwners.IsChecked = allOwners.IsChecked && owner.IsChecked;

                        owner.Tests.forEach(function(test) {
                            allOwners.Tests.push(test);
                        });
                    });
                    cs.ownersList.unshift(allOwners);
                    cs.FilterData.OwnerName = cs.ownersList[0].OwnerName;
                }

                cs.DisplayName = getDisplayName(cs.Parallel);

                cs.caseSelected = true;
                cs.preloadGet = false;

                //отменяем автоматическое обновление результатов
                //если таковой уже был запущен
                if (!!cs.stopInterval) {
                    $interval.cancel(cs.stopInterval);
                }

                //запускаем обновление результатов каждые 30 сек
                cs.stopInterval = $interval(cs.updateModel, 30000);

            }).error(function (response) {
                cs.preloadGet = false;
                showAlert(response.Message, false);
            });
        };

        //загружаем список прогонов
        $http.post('Autotest/GetParallels').success(function(response) {
            cs.results = response;

            cs.isload = true;
            cs.mainSpinner = false;

            _.each(cs.results, function (item) {
                item.DisplayName = getDisplayName(item);
            });

            //по умолчанию выбран первый
            cs.result = cs.results[0];
        }).error(function (response) {
            cs.isload = true;
            cs.mainSpinner = false;

            showAlert(response.Message, false);
        });

        //функция для обратного вызова из tab
        //при смене tab подгружать только один массив тестов для owner
        //остальным подставляю [], чтобы невидимые tabы не отрисовывали массив
        //и не нагружали страницу
        //обратный эффект - сами табы долго переключаются
        cs.showTests = (function (owner) {
            cs.ownersList.forEach(function(ownerX) {
                ownerX.tests = ownerX.OwnerName == owner.OwnerName ? ownerX.Tests : [];
                ownerX.ShowTests = ownerX.OwnerName == owner.OwnerName;

                if (ownerX.ShowTests) {
                    updateFilterData(ownerX);
                }
            });
        });

        function updateFilterData(owner) {
            cs.FilterData.All = 0;
            cs.FilterData.Checked = 0;
            cs.FilterData.NotChecked = 0;
            cs.FilterData.OwnerName = owner.OwnerName;
            owner.Tests.forEach(function (test) {
                if (test.IsChecked)
                    cs.FilterData.Checked++;
                else {
                    cs.FilterData.NotChecked++;
                }
                cs.FilterData.All++;
            });
        }

        //отображаемое имя прогона
        function getDisplayName(parallelObj) {
            var displayName = 'Запуск на адресе ' + parallelObj.Address + ', начат ' + cs.getDate(parallelObj.TimeStart, false);

            if (!!parallelObj.TimeEnd) {
                displayName += " конец " + cs.getDate(parallelObj.TimeEnd, false);
            }

            return displayName;
        };

        //отображаемое имя для теста
        cs.getDisplayNameTest = (function (testObj) {
            //чтобы оптимизировать вызовы фильтров для дат делаю только один раз
            //затем записываю в переменную и возвращаю уже её
            if (!!!testObj.DisplayName) {
                var displayName = 'Тест № <strong class="black_text">' + testObj.Name + '</strong>, начат ' + cs.getDate(testObj.TimeStart, true);

                if (!!testObj.TimeEnd) {
                    displayName += " конец " + cs.getDate(testObj.TimeEnd, true);
                }

                displayName += " (" + testObj.LoginName + ")";

                testObj.DisplayName = displayName;
                return testObj.DisplayName;
            } else {
                return testObj.DisplayName;
            }
        });

        //показывать или нет ошибки указанного теста
        cs.showErrors = (function (item) {
            if (item.ShowErrors) {
                //прятать массив, чтобы не нагружать страницу
                item.errors = [];
                item.ShowErrors = false;
            } else {
                //для оптимизации - так как все ошибки изначально скрыты
                //то не загружать их (подсовывать пустой массив)
                //и подставлять только если запросили
                item.errors = item.Errors;
                item.ShowErrors = true;
            }
        });

        //функция обратного вызова для проверки существования таба
        //нужна для динамического функционирования табов
        cs.existsOwner = (function(key, owner) {
            if (!!cs.ownersList[key]) {
                var ow = cs.ownersList[key];
                if(ow.OwnerName == owner.OwnerName)
                    if (ow.Tests.length > 0) 
                        return true;
            }

            return false;
        });

        cs.ownerFilter = (function(owner) {
            if (owner.Tests.length > 0) {
                if (cs.ShowErrorType == "All")
                    return false;

                var checked = 0;
                var notchecked = 0;
                owner.Tests.forEach(function(test) {
                    if (test.IsChecked)
                        checked++;
                    else {
                        notchecked++;
                    }
                });

                if (cs.ShowErrorType == "Checked" && checked != 0)
                    return false;

                if (cs.ShowErrorType == "NotChecked" && notchecked != 0)
                    return false;
            }

            return true;
        });

        //функция для динамического обновления результатов
        cs.updateModel = (function () {
            cs.preloadUpdate = true;
            $http.post('Autotest/GetParallel2', { 'parallelId': cs.Parallel.Id }).success(function (response) {

                //отменяем автометическое обновление результатов
                //если таковой уже был запущен
                if (!!cs.stopInterval) {
                    $interval.cancel(cs.stopInterval);
                    cs.stopInterval = false;
                }

                //обновляем основные данные для прогона
                cs.Parallel.TestsEnd = response.TestsEnd;
                cs.Parallel.TestsBegin = response.TestsBegin;
                cs.Parallel.TestsFailed = response.TestsFailed;
                cs.Parallel.TimeEnd = response.TimeEnd;
                cs.Parallel.ScreenPath = response.ScreenPath;
                cs.IsChecked = response.IsChecked;

                cs.DisplayName = getDisplayName(cs.Parallel);
                cs.result.DisplayName = getDisplayName(cs.Parallel);

                var selected = false;
                cs.ownersList.forEach(function (ownerX, indexX) {
                    if (ownerX.OwnerName == "Все") {
                        if (ownerX.ShowTests)
                            selected = true;

                        cs.ownersList.splice(indexX, 1);
                    }
                });

                //обновляем список owner'ов
                var index = cs.ownersList.length;
                while(index--) {
                    var owner = cs.ownersList[index];
                    //проверяем остались ли в модели все значения
                    //из ответа сервера - запоминаем индексы если найден
                    var exists = false;
                    var resIndex = 0;
                    response.Owners.forEach(function(resOwner, resIndexX) {
                        if (owner.OwnerName === resOwner.OwnerName) {
                            exists = true;
                            resIndex = resIndexX;
                        }
                    });

                    if (!exists) {
                        //если owner'а уже нет - удаляем его из модели
                        cs.ownersList.splice(index, 1);
                    } else {
                        //если owner остался - синхронизируем его список тестов
                        cs.ownersList[index].IsChecked = response.Owners[resIndex].IsChecked;

                        var tests = cs.ownersList[index].Tests;
                        var resTests = response.Owners[resIndex].Tests;

                        var indexTest = tests.length;
                        while(indexTest--) {
                            var test = tests[indexTest];
                            //проверяем остались ли в модели все значения
                            //из ответа сервера - запоминаем индексы если найден
                            var existsTest = false;
                            var resIndexTest = 0;
                            resTests.forEach(function (resTest, resIndexTestX) {
                                if (test.TestId === resTest.TestId) {
                                    existsTest = true;
                                    resIndexTest = resIndexTestX;
                                }
                            });

                            if (!existsTest) {
                                //если теста уже нет - удаляем его из модели
                                cs.ownersList[index].Tests.splice(indexTest, 1);
                            } else {
                                //у ошибок нет толком динамических параметров - копируем данные напрямую
                                cs.ownersList[index].Tests[indexTest].LoginName = response.Owners[resIndex].Tests[resIndexTest].LoginName;
                                cs.ownersList[index].Tests[indexTest].TimeStart = response.Owners[resIndex].Tests[resIndexTest].TimeStart;
                                cs.ownersList[index].Tests[indexTest].TimeEnd = response.Owners[resIndex].Tests[resIndexTest].TimeEnd;
                                cs.ownersList[index].Tests[indexTest].IsChecked = response.Owners[resIndex].Tests[resIndexTest].IsChecked;
                                cs.ownersList[index].Tests[indexTest].Errors = response.Owners[resIndex].Tests[resIndexTest].Errors;

                                //обновляем отображаемое имя
                                cs.ownersList[index].Tests[indexTest].DisplayName = false;
                                cs.getDisplayNameTest(cs.ownersList[index].Tests[indexTest]);

                                //если ошибка сейчас раскрыта - обновляем список для теста
                                if (!!cs.ownersList[index].Tests[indexTest].ShowErrors)
                                    cs.ownersList[index].Tests[indexTest].errors = cs.ownersList[index].Tests[indexTest].Errors;

                                //в конце удалить, чтобы остались те, которых не было в модели
                                response.Owners[resIndex].Tests.splice(resIndexTest, 1);
                            }
                        };

                        //те что остались после обработки тупо добавить
                        //ибо их не было изначально в модели
                        response.Owners[resIndex].Tests.forEach(function (testX, indexTestX) {
                            cs.ownersList[index].Tests.push(testX);
                            response.Owners[resIndex].Tests.splice(indexTestX, 1);
                        });

                        //если owner выбран на вкладке - обновляем его список тестов
                        if (!!cs.ownersList[index].ShowTests) {
                            cs.ownersList[index].tests = cs.ownersList[index].Tests;
                            updateFilterData(cs.ownersList[index]);
                        }

                        //в конце удалить, чтобы остались те, которых не было в модели
                        response.Owners.splice(resIndex, 1);
                    }
                };

                //те что остались после обработки тупо добавить
                //ибо их не было изначально в модели
                response.Owners.forEach(function(ownerX, indexX) {
                    cs.ownersList.push(ownerX);
                    response.Owners.splice(indexX, 1);
                });

                var allOwners = { OwnerName: "Все", IsChecked: true, Tests: [] };
                cs.ownersList.forEach(function (ownerX) {
                    allOwners.IsChecked = allOwners.IsChecked && ownerX.IsChecked;

                    ownerX.Tests.forEach(function (testX) {
                        allOwners.Tests.push(testX);
                    });
                });
                //если owner выбран на вкладке - обновляем его список тестов
                if (selected) {
                    allOwners.ShowTests = true;
                    allOwners.tests = allOwners.Tests;
                    updateFilterData(allOwners);
                }

                cs.ownersList.unshift(allOwners);

                //для динамического обновления табов
                cs.updateValue = true;
                cs.preloadUpdate = false;

                if (typeof (cs.sorted) !== "undefined")
                    cs.sortTests(cs.sorted);

                //запускаем обновление результатов каждые 30 сек
                cs.stopInterval = $interval(cs.updateModel, 30000);

            }).error(function (response) {
                cs.preloadUpdate = false;
                showAlert(response.Message, false);
            });
        });

        //загрузить скрин с сетевого пути - запрос на сервер, который
        //возвращает base64 - динамически вставляю в попап с тегом img (полноразмерная картинка)
        cs.loadScreen = (function (errorObj) {
            errorObj.preloadScreen = true;
            var address = (cs.Parallel.ScreenPath + errorObj.ScreenPath);
            $http.post('Autotest/GetScreen', { 'address': address }).success(function (response) {

                var popups = document.getElementsByClassName('b_popup ng-scope');
                for (var i = 0; i < popups.length; i++) {
                    popups[i].remove();
                }

                var screenBase64 = response.replace(/"/g, '');

                var img = document.createElement("img");
                img.src = 'data:image/png;base64,' + screenBase64;
                var text = "Скрин";
                img.style.float = 'none';
                img.style.width = '100%';
                img.style.height = 'auto';

                img.onload = function () {

                    var block = angular.element('<div class="b_popup" style="width:' + img.width + 'px;" draggable/>')
                    .append($('<h2 class="b_header"/>').text(text).append($('<div class="b_close outer-div" />').append($('<div class="b_close inner-div">×</div>'))))
                    .append($('<div class="b_content"/>').append(img));

                    block.bPopup({ appendTo: 'body' });
                    $compile(block)(cs);

                    var background = document.getElementsByClassName('bModal __bPopup1');
                    if (background.length > 0)
                        background[0].remove();
                };

                
                
                errorObj.preloadScreen = false;

            }).error(function (response) {
                errorObj.preloadScreen = false;
                showAlert(response.Message, false);
            });

        });

        //дата в читабельном виде
        cs.getDate = (function (date, seconds) {
            return $filter('jsonDate')(date, 'dd.MM.yyyy в HH:mm' + (seconds ? ':ss' : ''));
        });

        //проверка валидации нового сетевого пути
        cs.isValidPath = (function () {
            if (!!cs.Parallel) {
                var result = cs.Parallel.NewScreenPath.Value.match(/^\\\\[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}(\\[А-Яа-яA-Za-z_\-\s0-9\.]+)+\\$/);

                if (!cs.Parallel.NewScreenPath.isFirstClick) {
                    cs.Parallel.NewScreenPath.Valid = result;
                }
                return result;

            } else {
                return true;
            }
        });

        //отметить ошибку из теста проверенной (запрос на сервер + обновление статуса в модели js)
        cs.markCheckError = (function (errorObj) {
            errorObj.preloadCheck = true;
            $http.post('Autotest/CheckError', { 'errorId': errorObj.ErrorId }).success(function (response) {

                errorObj.IsChecked = true;
                refreshParallelCheck();
                errorObj.preloadCheck = false;
                showAlert(response, true);

            }).error(function (response) {
                errorObj.preloadCheck = false;
                showAlert(response.Message, false);
            });
        });

        //отметить ошибку из теста НЕ проверенной (запрос на сервер + обновление статуса в модели js)
        cs.markUnCheckError = (function (errorObj) {
            errorObj.preloadCheck = true;
            $http.post('Autotest/UnCheckError', { 'errorId': errorObj.ErrorId }).success(function (response) {

                errorObj.IsChecked = false;
                refreshParallelCheck();
                errorObj.preloadCheck = false;
                showAlert(response, true);

            }).error(function (response) {
                errorObj.preloadCheck = false;
                showAlert(response.Message, false);
            });
        });

        //изменить сетевой путь к скринам (запрос на сервер + обновление переменной в модели js)
        cs.changeParallelAddress = (function () {

            cs.Parallel.NewScreenPath.Valid = cs.isValidPath();
            cs.Parallel.NewScreenPath.isFirstClick = false;
            if (!cs.Parallel.NewScreenPath.Valid)
                return;

            cs.Parallel.NewScreenPath.preloadSave = true;
            $http.post('Autotest/CopyScreen', { 'parallelId': cs.Parallel.Id, 'screenPath': cs.Parallel.NewScreenPath.Value }).success(function (response) {

                cs.Parallel.ScreenPath = cs.Parallel.NewScreenPath.Value;
                cs.Parallel.NewScreenPath.preloadSave = false;
                layers.popLastLayer();
                showAlert(response, true);

            }).error(function (response) {
                cs.Parallel.NewScreenPath.preloadSave = false;
                showAlert(response.Message, false);
            });
        });

        //удалить ошибку из теста (запрос на сервер + обновление статуса в модели js)
        cs.deleteError = (function (errorObj, testObj) {
            errorObj.preloadDelete = true;
            $http.post('Autotest/DeleteError', { 'errorId': errorObj.ErrorId }).success(function (response) {

                testObj.Errors.forEach(function (error, index) {
                    if (error.ErrorId == errorObj.ErrorId)    
                        testObj.Errors.splice(index, 1);
                });

                refreshParallelCheck();
                showAlert(response, true);
                errorObj.preloadCheck = false;

            }).error(function (response) {
                errorObj.preloadCheck = false;
                showAlert(response.Message, false);
            });
        });

        //удалить прогон (запрос на сервер + очистка модели js)
        //так же убираем значение из селекта прогонов
        //требует конфёрма
        cs.deleteParallel = (function () {

            var r = confirm("Удалить прогон?");

            if (r === true) {
                cs.Parallel.preloadDelete = true;
                $http.post('Autotest/DeleteParallel', { 'parallelId': cs.Parallel.Id }).success(function (response) {

                    //отменяем автоматическое обновление результатов
                    //если таковой уже был запущен
                    if (!!cs.stopInterval) {
                        $interval.cancel(cs.stopInterval);
                    }

                    cs.caseSelected = false;
                    cs.ownersList = [];

                    cs.results.forEach(function(parallel, index) {
                        if (parallel.Id == cs.Parallel.Id)
                            cs.results.splice(index, 1);
                    });

                    if (cs.results.length > 0)
                        cs.result = cs.results[0];
                    else {
                        cs.result = [];
                    }

                    showAlert(response, true);
                    cs.updateValue = true;
                    cs.Parallel.preloadDelete = false;

                }).error(function(response) {
                    cs.Parallel.preloadDelete = false;
                    showAlert(response.Message, false);
                });
            }
        });

        //пометить тест проверенным (запрос на сервер + обновление статуса в модели js)
        cs.markCheckTest = (function (testObj) {
            testObj.preloadCheck = true;
            $http.post('Autotest/CheckTest', { 'parallelId': cs.Parallel.Id, 'testId': testObj.TestId }).success(function (response) {

                testObj.Errors.forEach(function (error) {
                    error.IsChecked = true;
                });

                testObj.IsChecked = true;
                refreshParallelCheck();
                showAlert(response, true);
                testObj.preloadCheck = false;

            }).error(function (response) {
                testObj.preloadCheck = false;
                showAlert(response.Message, false);
            });
        });

        //закрываем дропдаун
        cs.closeDropdownForm = (function () {
            layers.popLastLayer();
        });

        //функция для обновления отметок о статусе проверки
        //(зеленые и красные кружочки)
        function refreshParallelCheck() {
            var checked = true;
            cs.ownersList.forEach(function (ownerObj) {
                var ownerChecked = true;
                ownerObj.Tests.forEach(function (testObj) {
                    var testChecked = true;
                    testObj.Errors.forEach(function (error) {
                        if (!error.IsChecked) {
                            testChecked = false;
                            ownerChecked = false;
                            checked = false;
                        }
                    });
                    testObj.IsChecked = testChecked;
                });
                ownerObj.IsChecked = ownerChecked;

                if (cs.FilterData.OwnerName == ownerObj.OwnerName)
                    updateFilterData(ownerObj);
            });
            cs.IsChecked = checked;
        };

        cs.sortTests = (function (type) {
            cs.preloadSort = true;
            cs.ownersList.forEach(function (ownerObj) {
                ownerObj.Tests.sort(function (a, b) {
                    if (a.Name < b.Name) return !!type ? -1 : 1;
                    if (a.Name > b.Name) return !!type ? 1 : -1;

                    return 0;
                });
            });
            cs.sorted = type;
            cs.preloadSort = false;
        });

        //function showErrorsInFirstLoad() {
        //    cs.ownersList.forEach(function (ownerObj) {
        //        ownerObj.Tests.forEach(function (testObj) {
        //            if (!testObj.IsChecked) {
        //                testObj.ShowErrors = true;
        //                testObj.errors = testObj.Errors;
        //            }
        //        });
        //    });
        //}

        //функция для показа алертов
        function showAlert(messageOutput, green) {
            if (!!!messageOutput)
                messageOutput = "Похоже отвалилась авторизация, перелогинтесь";

            if (messageOutput[0] === "\"")
                messageOutput = messageOutput.substr(1, messageOutput.length-2);

            //в зависимости от типа - зеленый или красный алерт
            var alertBox = $(green == true ? '.ajax-alert-box' : '.ajax-error-box').text(messageOutput).css('top', '70px');

            var timer = 0;
            var handleChange = function () {
                alertBox.fadeTo('fast', 1, function () {
                    timer = setTimeout(function () {
                        alertBox.fadeTo('fast', 0, function () {
                            alertBox.css('top', '-100px');
                        });
                    }, 3000);
                });

            };
            clearTimeout(timer);
            handleChange();
            alertBox.hover(function () { clearTimeout(timer); }, handleChange);
        }
    };
})();
