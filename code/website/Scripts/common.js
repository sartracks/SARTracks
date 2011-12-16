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

function updateSort(table, sortList) {
    if (sortList == null) {
        sortList = [];
    }
    table.trigger("update");
    table.trigger("sorton", [sortList]);
    table.trigger("applyWidgets");
}

function formatDateTime(format, date, settings) {
    if (!date) { return "" }
    // var dayNamesShort = (settings ? settings.dayNamesShort : null) || this._defaults.dayNamesShort;
    // var dayNames = (settings ? settings.dayNames : null) || this._defaults.dayNames;
    // var monthNamesShort = (settings ? settings.monthNamesShort : null) || this._defaults.monthNamesShort;
    // var monthNames = (settings ? settings.monthNames : null) || this._defaults.monthNames;
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
                    //    case "o": var doy = date.getDate(); for (var m = date.getMonth() - 1; m >= 0; m--) { doy += this._getDaysInMonth(date.getFullYear(), m) } output += formatNumber("o", doy, 3); break; 
                    case "m": output += formatNumber("m", date.getMonth() + 1, 2); break;
                    //    case "M": output += formatName("M", date.getMonth(), monthNamesShort, monthNames); break; 
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
    buttons[buttonText] =  function () {
                $('[data-val]').removeClass("ui-state-error");
                var dialog = $(this);
                global_showProgress();
                $.ajax({ type: 'POST', url: submitUrl, data: $(this).find("form").serialize(), dataType: 'json',
                    success: function (data) {
                        global_hideProgress();
                        if (data.Errors.length > 0) {
                            alert('validation errors');
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