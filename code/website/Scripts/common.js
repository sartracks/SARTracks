/* Copyright 2011 Matt Cosand and others (see AUTHORS.TXT)
*
* This file is part of SARTracks.
*
*  SARTracks is free software: you can redistribute it and/or modify
*  it under the terms of the GNU Affero General Public License as published by
*  the Free Software Foundation, either version 3 of the License, or
*  (at your option) any later version.
*
*  SARTracks is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU Affero General Public License for more details.
*
*  You should have received a copy of the GNU Affero General Public License
*  along with SARTracks.  If not, see <http://www.gnu.org/licenses/>.
*/

Date.prototype.toJSON = function (key) {
    return isFinite(this.valueOf()) ? '\/Date(' + this.getTime() + this.getUTCOffset() + ')\/' : null;
};
// IE7 .getUTCOffset returns 'undefined0700'
Date.prototype.getUTCOffset = function () {
    var f = this.getTimezoneOffset();
    return (f < 0 ? '+' : '-') + ((f / 60 < 10) ? '0' : '') + f / 60 + ((f % 60 < 10) ? '0' : '') + f % 60;
};

function ifDefined(value) { return (value == undefined) ? "" : value; }
function ifNumeric(value) { return isNaN(value) ? "" : value; }

function fixTime(item, fields) {
    if (fields == undefined) {
        fields = ['T'];
    }
    for (var i in fields) {
        if (item[fields[i]] != undefined) {
            var tmp = /\/Date\((\d+).*/.exec(item[fields[i]]);
            item[fields[i]] = (tmp == null) ? Date.parse('1800-01-01') : new Date(parseInt(tmp[1]));
        }
    }
}

function parseCoordinate(coord) {
    if (coord == undefined || coord == "") return null;

    var m = /^\-?(\d{2,3}(\.\d+)?)$/.exec(coord);
    if (m != null) { return m[1]; }

    m = /^\-?(\d{2,3}) (\d{1,2}(\.\d+)?)$/.exec(coord);
    if (m != null) { return Math.floor(m[1]) + m[2] / 60.0; }

    m = /\-?(\d{2,3}) (\d{1,2}) (\d{1,2}(\.\d+)?)$/.exec(coord);
    if (m != null) { return Math.floor(m[1]) + (Math.floor(m[2]) + m[3] / 60.0) / 60.0; }

    return undefined;
}

function isDefined(variable) {
    return (typeof (window[variable]) == "undefined") ? false : true;
}

function colorTable(table) {
    table.find('tbody tr').filter(':even').removeClass('odd');
    table.find('tbody tr').filter(':odd').addClass('odd');
}

function updateSort(table, sortList) {
    if (table.find('tbody tr').length == 0) return;
    if (sortList == null && table.length > 0 && table[0].config != null && (table[0].config.sortList.length != 0))
    {
        sortList = table[0].config.sortList
    }
    if (sortList == null && table.data('sort') != null) {
        sortList = table.data('sort');
    }
    if (sortList == null) { sortList = [] }

    table.trigger("update");
    table.trigger("sorton", [sortList]);
    table.trigger("applyWidgets");
}

function formatDateTime(format, date, settings) {
    if (!date) { return "" }
    var lookAhead = function (match) {
        var matches = (iFormat + 1 < format.length && format.charAt(iFormat + 1) == match);
        if (matches) { iFormat++ } return matches
    };
    var formatNumber = function (match, value, len) {
        var num = "" + value;
        if (lookAhead(match)) {
            while (num.length < len) {
                num = "0" + num
            }
        }
        return num
    };
    var formatName = function (match, value, shortNames, longNames) {
        return (lookAhead(match) ? longNames[value] : shortNames[value])
    };
    var output = "";
    var literal = false;
    if (date) {
        for (var iFormat = 0; iFormat < format.length; iFormat++) {
            if (literal) {
                if (format.charAt(iFormat) == "'" && !lookAhead("'")) {
                    literal = false
                } else {
                    output += format.charAt(iFormat)
                }
            } else {
                switch (format.charAt(iFormat)) {
                    case "d": output += formatNumber("d", date.getDate(), 2); break;
                    case "D": output += formatName("D", date.getDay(), ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]); break;
                    case "m": output += formatNumber("m", date.getMonth() + 1, 2); break;
                    case "y": output += (lookAhead("y") ? date.getFullYear() : (date.getYear() % 100 < 10 ? "0" : "") + date.getYear() % 100); break;
                    case "@": output += date.getTime(); break;
                    case "'": if (lookAhead("'")) { output += "'" } else { literal = true } break;
                    case 'H': output += formatNumber("H", date.getHours(), 2); break;
                    case 'i': output += formatNumber('i', date.getMinutes(), 2); break;
                    default: output += format.charAt(iFormat)
                }
            }
        }
    }
    return output
};

var managementLoaded = new Array();
function loadManagement(url, data, afterLoad) {
    if (!managementLoaded[url]) {
        global_showProgress();
        $.ajax({ type: 'GET', url: url, data: data,
            success: function (html) {
                if (managementLoaded[url]) return;
                managementLoaded[url] = true;
                $('body').append(html);
                global_hideProgress();
                if (afterLoad) afterLoad();
            }
        });

    }
    else {
        if (afterLoad) afterLoad();
    }
}

function setupFormDialog(dialogDiv, height, width, buttonText, submitUrl, onSuccess) {
    var buttons = new Object();
    dialogDiv.find("form").submit(function () {
        dialogDiv.parent().find(".ui-dialog-buttonset button").first().click();
      return false; });

    // When changing a field, assume we're correcting any errors on the field
      dialogDiv.find("input").change(function () { $(this).removeClass('input-validation-error'); dialogDiv.find("[data-valmsg-for='" + this.name + "']").addClass('field-validation-valid').removeClass('field-validation-error'); });

    buttons[buttonText] = function () {
        var dialog = $(this);
        if (dialog.find('.field-validation-error').length > 0) { alert('Please fix errors'); return; }

        global_showProgress();
        $.ajax({ type: 'POST', url: submitUrl, data: $(this).find("form").serialize(), dataType: 'json',
            success: function (data) {
                global_hideProgress();
                if (data.Errors.length > 0) {
                    for (var i in data.Errors) {
                          $('[data-valmsg-for="' + data.Errors[i].Property + '"]').html(data.Errors[i].Error).addClass('field-validation-error').removeClass('field-validation-valid');
                          $('[name="' + data.Errors[i].Property + '"]').addClass('input-validation-error');
                    }
                }
                else {
                    var doClose = true;
                    if (onSuccess) doClose = onSuccess(data.Result);
                    if (doClose) dialog.dialog("close");
                }
            },
            error: function () {
                global_hideProgress();
                alert('An unknown error occurred. You may want to try submitting again.');
            },
            statusCode: {
                403: function () {
                    global_hideProgress();
                    alert('not logged in. Please refresh');
                }
            }
        });
    };

    buttons["Cancel"] = function () {
            $(this).dialog("close");
        };

    dialogDiv.dialog({
        autoOpen: false,
        height: height,
        width: width,
        modal: true,
        buttons: buttons,
        close: function () {
            $('[data-val]').val("").removeClass("ui-state-error");
        }
    });
}

ko.bindingHandlers.triggerUpdate = {
    update: function (element, valueAccessor) {
        ko.utils.unwrapObservable(valueAccessor()); //need to just access the observable to create the subscription
        colorTable($(element));
    }
}

function setupFormDialog2(dialogDiv, height, width, buttonText, submitUrl, onSuccess) {
    var buttons = new Object();
    dialogDiv.find("form").submit(function () {
        dialogDiv.parent().find(".ui-dialog-buttonset button").first().click();
        return false;
    });

    buttons[buttonText] = function () {
        var dialog = $(this);
        var f = dialog.find('form');
        f.find('.error').removeClass('error');
        f.find('.validmsg').hide();

        global_showProgress();
        $.ajax({ type: 'POST', url: submitUrl, data: $(this).find("form").serialize(), dataType: 'json',
            complete: function () { global_hideProgress(); },
            success: function (data) {
                global_hideProgress();

                if (data.Errors.length > 0) {
                    for (i in data.Errors) {
                        var e = data.Errors[i];
                        if (e.p == null && e.m != null) {
                            f.find('[name="validateTips"]').addClass('error').html(e.m).show();
                        }
                        else if (e.m != null && e.m != null) {
                            f.find('[validates="' + e.p + '"]').addClass('error').html(e.m).show();
                            f.find('[name="' + e.p + '"]').addClass('error');
                        }
                    }
                }
                else {
                    if (onSuccess != null) onSuccess(data.Result);
                    dialog.dialog("close");
                }
            },
            error: function (a, b, c) {
                if (c == "Forbidden") { alert('not logged in. Please refresh the page'); return; }

                alert('An unknown error occurred. You may want to try submitting again.' + a + ':' + b + ':' + c);
            }
        });
    };

    buttons["Cancel"] = function () {
        $(this).dialog("close");
    };

    dialogDiv.dialog({
        autoOpen: false,
        height: height,
        width: width,
        modal: true,
        buttons: buttons,
        close: function () {
        }
    });
}

var formsSetup = false;
function formsInit() {
    if (formsSetup) return;

    formsSetup = true;
    $("#formsDelete").dialog({
        autoOpen: false,
        height: 180,
        width: 450,
        modal: true,
        buttons: {
            'Delete': function () {
                $('.ui-dialog-buttonpane button').attr('disabled', 'true').addClass('ui-state-disabled');
                var d = $(this);

                var url = d[0].url;

                $.ajax({ type: 'POST', url: d[0].url, data: { q: d[0].delId }, dataType: 'json',
                    success: function (data) {
                        if (data.Errors.length == 0) {
                            d.dialog('close');
                            if (d[0].onSuccess != undefined) d[0].onSuccess(d[0].delId);
                        }
                    },
                    //error: handleDataActionError,
                    complete: function (request, status) {
                        $('.ui-dialog-buttonpane button').removeAttr('disabled').removeClass('ui-state-disabled');
                    }
                });
            },
            Cancel: function () {
                $(this).dialog('close');
            }
        }
    });
}

function doDeleteForm(title, url, id, msg, onSuccess) {
    var del = $('#formsDelete');
    del[0].onSuccess = onSuccess;
    del[0].url = url;
    del[0].delId = id;

    $('#ui-dialog-title-formsDelete').html("Delete " + title);
    $('#formsDeleteTitle').html(msg);
    del.dialog('open');
    del.dialog('option', 'position', 'center');
}

function wireEditDelete(objectType, url, loadMethod) {
    $('body').on('click', '.Delete' + objectType, function (f) {
        var t = $(f.currentTarget);

        doDeleteForm(objectType, url, t.attr('delid'), t.attr('delmsg'), loadMethod);
    });
    $('body').on("click", ".Edit" + objectType, function (f) {
        var j = ko.toJS(ko.dataFor(f.currentTarget));
        var m = ko.mapping.fromJS(j);
        ko.applyBindings(m, $('#' + objectType + 'Dialog')[0]);
        $('#' + objectType + 'Dialog').dialog("open");
    });
}