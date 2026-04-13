/* ====================================================
 * Company: Unity Technologies
 * Author:  Rickard Andersson, rickard@unity3d.com
 *
   TABLE OF CONTENTS
   1. Plugins
   2. Global variables
   3. Sidebar
   4. Start-up
   5. Window resize checks
   6. UI events
   7. Global functions
 *
   Note that this core.js is not quite the same as the 
   core.js file used by the manual side of the house,
   though there is quite a bit of overlap.
======================================================= */

/****************************************
  ==== 1. PLUGINS
****************************************/

/* Modernizr v2.5.3 www.modernizr.com */
window.Modernizr=function(a,b,c){function D(a){j.cssText=a}function E(a,b){return D(n.join(a+";")+(b||""))}function F(a,b){return typeof a===b}function G(a,b){return!!~(""+a).indexOf(b)}function H(a,b){for(var d in a){if(j[a[d]]!==c){return b=="pfx"?a[d]:true}}return false}function I(a,b,d){for(var e in a){var f=b[a[e]];if(f!==c){if(d===false)return a[e];if(F(f,"function")){return f.on(d||b)}return f}}return false}function J(a,b,c){var d=a.charAt(0).toUpperCase()+a.substr(1),e=(a+" "+p.join(d+" ")+d).split(" ");if(F(b,"string")||F(b,"undefined")){return H(e,b)}else{e=(a+" "+q.join(d+" ")+d).split(" ");return I(e,b,c)}}function L(){e["input"]=function(c){for(var d=0,e=c.length;d<e;d++){u[c[d]]=!!(c[d]in k)}if(u.list){u.list=!!(b.createElement("datalist")&&a.HTMLDataListElement)}return u}("autocomplete autofocus list placeholder max min multiple pattern required step".split(" "));e["inputtypes"]=function(a){for(var d=0,e,f,h,i=a.length;d<i;d++){k.setAttribute("type",f=a[d]);e=k.type!=="text";if(e){k.value=l;k.style.cssText="position:absolute;visibility:hidden;";if(/^range$/.test(f)&&k.style.WebkitAppearance!==c){g.appendChild(k);h=b.defaultView;e=h.getComputedStyle&&h.getComputedStyle(k,null).WebkitAppearance!=="textfield"&&k.offsetHeight!==0;g.removeChild(k)}else if(/^(search|tel)$/.test(f)){}else if(/^(url|email)$/.test(f)){e=k.checkValidity&&k.checkValidity()===false}else if(/^color$/.test(f)){g.appendChild(k);g.offsetWidth;e=k.value!=l;g.removeChild(k)}else{e=k.value!=l}}t[a[d]]=!!e}return t}("search tel url email datetime date month week time datetime-local number range color".split(" "))}var d="2.5.3",e={},f=true,g=b.documentElement,h="modernizr",i=b.createElement(h),j=i.style,k=b.createElement("input"),l=":)",m={}.toString,n=" -webkit- -moz- -o- -ms- ".split(" "),o="Webkit Moz O ms",p=o.split(" "),q=o.toLowerCase().split(" "),r={svg:"http://www.w3.org/2000/svg"},s={},t={},u={},v=[],w=v.slice,x,y=function(a,c,d,e){var f,i,j,k=b.createElement("div"),l=b.body,m=l?l:b.createElement("body");if(parseInt(d,10)){while(d--){j=b.createElement("div");j.id=e?e[d]:h+(d+1);k.appendChild(j)}}f=["&#173;","<style>",a,"</style>"].join("");k.id=h;m.innerHTML+=f;m.appendChild(k);if(!l){m.style.background="";g.appendChild(m)}i=c(k,a);!l?m.parentNode.removeChild(m):k.parentNode.removeChild(k);return!!i},z=function(b){var c=a.matchMedia||a.msMatchMedia;if(c){return c(b).matches}var d;y("@media "+b+" { #"+h+" { position: absolute; } }",function(b){d=(a.getComputedStyle?getComputedStyle(b,null):b.currentStyle)["position"]=="absolute"});return d},A=function(){function d(d,e){e=e||b.createElement(a[d]||"div");d="on"+d;var f=d in e;if(!f){if(!e.setAttribute){e=b.createElement("div")}if(e.setAttribute&&e.removeAttribute){e.setAttribute(d,"");f=F(e[d],"function");if(!F(e[d],"undefined")){e[d]=c}e.removeAttribute(d)}}e=null;return f}var a={select:"input",change:"input",submit:"form",reset:"form",error:"img",load:"img",abort:"img"};return d}();var B={}.hasOwnProperty,C;if(!F(B,"undefined")&&!F(B.call,"undefined")){C=function(a,b){return B.call(a,b)}}else{C=function(a,b){return b in a&&F(a.constructor.prototype[b],"undefined")}}if(!Function.prototype.on){Function.prototype.on=function(b){var c=this;if(typeof c!="function"){throw new TypeError}var d=w.call(arguments,1),e=function(){if(this instanceof e){var a=function(){};a.prototype=c.prototype;var f=new a;var g=c.apply(f,d.concat(w.call(arguments)));if(Object(g)===g){return g}return f}else{return c.apply(b,d.concat(w.call(arguments)))}};return e}}var K=function(c,d){var f=c.join(""),g=d.length;y(f,function(c,d){var f=b.styleSheets[b.styleSheets.length-1],h=f?f.cssRules&&f.cssRules[0]?f.cssRules[0].cssText:f.cssText||"":"",i=c.childNodes,j={};while(g--){j[i[g].id]=i[g]}e["touch"]="ontouchstart"in a||a.DocumentTouch&&b instanceof DocumentTouch||(j["touch"]&&j["touch"].offsetTop)===9;e["csstransforms3d"]=(j["csstransforms3d"]&&j["csstransforms3d"].offsetLeft)===9&&j["csstransforms3d"].offsetHeight===3;e["generatedcontent"]=(j["generatedcontent"]&&j["generatedcontent"].offsetHeight)>=1;e["fontface"]=/src/i.test(h)&&h.indexOf(d.split(" ")[0])===0},g,d)}(['@font-face {font-family:"font";src:url("https://")}',["@media (",n.join("touch-enabled),("),h,")","{#touch{top:9px;position:absolute}}"].join(""),["@media (",n.join("transform-3d),("),h,")","{#csstransforms3d{left:9px;position:absolute;height:3px;}}"].join(""),['#generatedcontent:after{content:"',l,'";visibility:hidden}'].join("")],["fontface","touch","csstransforms3d","generatedcontent"]);s["flexbox"]=function(){return J("flexOrder")};s["flexbox-legacy"]=function(){return J("boxDirection")};s["canvas"]=function(){var a=b.createElement("canvas");return!!(a.getContext&&a.getContext("2d"))};s["canvastext"]=function(){return!!(e["canvas"]&&F(b.createElement("canvas").getContext("2d").fillText,"function"))};s["webgl"]=function(){try{var d=b.createElement("canvas"),e;e=!!(a.WebGLRenderingContext&&(d.getContext("experimental-webgl")||d.getContext("webgl")));d=c}catch(f){e=false}return e};s["touch"]=function(){return e["touch"]};s["geolocation"]=function(){return!!navigator.geolocation};s["postmessage"]=function(){return!!a.postMessage};s["websqldatabase"]=function(){return!!a.openDatabase};s["indexedDB"]=function(){return!!J("indexedDB",a)};s["hashchange"]=function(){return A("hashchange",a)&&(b.documentMode===c||b.documentMode>7)};s["history"]=function(){return!!(a.history&&history.pushState)};s["draganddrop"]=function(){var a=b.createElement("div");return"draggable"in a||"ondragstart"in a&&"ondrop"in a};s["websockets"]=function(){for(var b=-1,c=p.length;++b<c;){if(a[p[b]+"WebSocket"]){return true}}return"WebSocket"in a};s["rgba"]=function(){D("background-color:rgba(150,255,150,.5)");return G(j.backgroundColor,"rgba")};s["hsla"]=function(){D("background-color:hsla(120,40%,100%,.5)");return G(j.backgroundColor,"rgba")||G(j.backgroundColor,"hsla")};s["multiplebgs"]=function(){D("background:url(https://),url(https://),red url(https://)");return/(url\s*\(.*?){3}/.test(j.background)};s["backgroundsize"]=function(){return J("backgroundSize")};s["borderimage"]=function(){return J("borderImage")};s["borderradius"]=function(){return J("borderRadius")};s["boxshadow"]=function(){return J("boxShadow")};s["textshadow"]=function(){return b.createElement("div").style.textShadow===""};s["opacity"]=function(){E("opacity:.55");return/^0.55$/.test(j.opacity)};s["cssanimations"]=function(){return J("animationName")};s["csscolumns"]=function(){return J("columnCount")};s["cssgradients"]=function(){var a="background-image:",b="gradient(linear,left top,right bottom,from(#9f9),to(white));",c="linear-gradient(left top,#9f9, white);";D((a+"-webkit- ".split(" ").join(b+a)+n.join(c+a)).slice(0,-a.length));return G(j.backgroundImage,"gradient")};s["cssreflections"]=function(){return J("boxReflect")};s["csstransforms"]=function(){return!!J("transform")};s["csstransforms3d"]=function(){var a=!!J("perspective");if(a&&"webkitPerspective"in g.style){a=e["csstransforms3d"]}return a};s["csstransitions"]=function(){return J("transition")};s["fontface"]=function(){return e["fontface"]};s["generatedcontent"]=function(){return e["generatedcontent"]};s["video"]=function(){var a=b.createElement("video"),c=false;try{if(c=!!a.canPlayType){c=new Boolean(c);c.ogg=a.canPlayType('video/ogg; codecs="theora"').replace(/^no$/,"");c.h264=a.canPlayType('video/mp4; codecs="avc1.42E01E"').replace(/^no$/,"");c.webm=a.canPlayType('video/webm; codecs="vp8, vorbis"').replace(/^no$/,"")}}catch(d){}return c};s["audio"]=function(){var a=b.createElement("audio"),c=false;try{if(c=!!a.canPlayType){c=new Boolean(c);c.ogg=a.canPlayType('audio/ogg; codecs="vorbis"').replace(/^no$/,"");c.mp3=a.canPlayType("audio/mpeg;").replace(/^no$/,"");c.wav=a.canPlayType('audio/wav; codecs="1"').replace(/^no$/,"");c.m4a=(a.canPlayType("audio/x-m4a;")||a.canPlayType("audio/aac;")).replace(/^no$/,"")}}catch(d){}return c};s["localstorage"]=function(){try{localStorage.setItem(h,h);localStorage.removeItem(h);return true}catch(a){return false}};s["sessionstorage"]=function(){try{sessionStorage.setItem(h,h);sessionStorage.removeItem(h);return true}catch(a){return false}};s["webworkers"]=function(){return!!a.Worker};s["applicationcache"]=function(){return!!a.applicationCache};s["svg"]=function(){return!!b.createElementNS&&!!b.createElementNS(r.svg,"svg").createSVGRect};s["inlinesvg"]=function(){var a=b.createElement("div");a.innerHTML="<svg/>";return(a.firstChild&&a.firstChild.namespaceURI)==r.svg};s["smil"]=function(){return!!b.createElementNS&&/SVGAnimate/.test(m.call(b.createElementNS(r.svg,"animate")))};s["svgclippaths"]=function(){return!!b.createElementNS&&/SVGClipPath/.test(m.call(b.createElementNS(r.svg,"clipPath")))};for(var M in s){if(C(s,M)){x=M.toLowerCase();e[x]=s[M]();v.push((e[x]?"":"no-")+x)}}e.input||L();e.addTest=function(a,b){if(typeof a=="object"){for(var d in a){if(C(a,d)){e.addTest(d,a[d])}}}else{a=a.toLowerCase();if(e[a]!==c){return e}b=typeof b=="function"?b():b;g.className+=" "+(b?"":"no-")+a;e[a]=b}return e};D("");i=k=null;(function(a,b){function g(a,b){var c=a.createElement("p"),d=a.getElementsByTagName("head")[0]||a.documentElement;c.innerHTML="x<style>"+b+"</style>";return d.insertBefore(c.lastChild,d.firstChild)}function h(){var a=k.elements;return typeof a=="string"?a.split(" "):a}function i(a){var b={},c=a.createElement,e=a.createDocumentFragment,f=e();a.createElement=function(a){var e=(b[a]||(b[a]=c(a))).cloneNode();return k.shivMethods&&e.canHaveChildren&&!d.test(a)?f.appendChild(e):e};a.createDocumentFragment=Function("h,f","return function(){"+"var n=f.cloneNode(),c=n.createElement;"+"h.shivMethods&&("+h().join().replace(/\w+/g,function(a){b[a]=c(a);f.createElement(a);return'c("'+a+'")'})+");return n}")(k,f)}function j(a){var b;if(a.documentShived){return a}if(k.shivCSS&&!e){b=!!g(a,"article,aside,details,figcaption,figure,footer,header,hgroup,nav,section{display:block}"+"audio{display:none}"+"canvas,video{display:inline-block;*display:inline;*zoom:1}"+"[hidden]{display:none}audio[controls]{display:inline-block;*display:inline;*zoom:1}"+"mark{background:#FF0;color:#000}")}if(!f){b=!i(a)}if(b){a.documentShived=b}return a}var c=a.html5||{};var d=/^<|^(?:button|form|map|select|textarea)$/i;var e;var f;(function(){var a=b.createElement("a");a.innerHTML="<xyz></xyz>";e="hidden"in a;f=a.childNodes.length==1||function(){try{b.createElement("a")}catch(a){return true}var c=b.createDocumentFragment();return typeof c.cloneNode=="undefined"||typeof c.createDocumentFragment=="undefined"||typeof c.createElement=="undefined"}()})();var k={elements:c.elements||"abbr article aside audio bdi canvas data datalist details figcaption figure footer header hgroup mark meter nav output progress section summary time video",shivCSS:!(c.shivCSS===false),shivMethods:!(c.shivMethods===false),type:"default",shivDocument:j};a.html5=k;j(b)})(this,b);e._version=d;e._prefixes=n;e._domPrefixes=q;e._cssomPrefixes=p;e.mq=z;e.hasEvent=A;e.testProp=function(a){return H([a])};e.testAllProps=J;e.testStyles=y;e.prefixed=function(a,b,c){if(!b){return J(a,"pfx")}else{return J(a,b,c)}};g.className=g.className.replace(/(^|\s)no-js(\s|$)/,"$1$2")+(f?" js "+v.join(" "):"");return e}(this,this.document)

/* jQuery Cookie Plugin v1.3 */
jQuery.cookie=function(a,b,c){if(arguments.length>1&&String(b)!=="[object Object]"){c=jQuery.extend({},c);if(b===null||b===undefined){c.expires=-1}if(typeof c.expires==="number"){var d=c.expires,e=c.expires=new Date;e.setDate(e.getDate()+d)}b=String(b);return document.cookie=[encodeURIComponent(a),"=",c.raw?b:encodeURIComponent(b),c.expires?"; expires="+c.expires.toUTCString():"",c.path?"; path="+c.path:"",c.domain?"; domain="+c.domain:"",c.secure?"; secure":""].join("")}c=b||{};var f,g=c.raw?function(a){return a}:decodeURIComponent;return(f=(new RegExp("(?:^|; )"+encodeURIComponent(a)+"=([^;]*)")).exec(document.cookie))?g(f[1]):null}

/****************************************
  ==== 2. GLOBAL VARIABLES
****************************************/

var defaultScriptLanguage = 'CS',
    scriptLanguages = ['JS', 'CS'],
    scriptlang = null,
    offline = (location.href.indexOf('docs.unity3d.com') == -1 && location.href.indexOf('docs.hq.unity3d.com') == -1) ? true : false;

if (!offline) {
    // internal analytics
    $('<script>').appendTo('head').attr('src', 'https://store.unity.com/themes/contrib/unity_base/js/unity-cdp.js');

    $('<link>').appendTo('head').attr({type : 'text/css', rel : 'stylesheet'}).attr('href', 'https://fonts.googleapis.com/css?family=Open+Sans:400,700,400italic');
    $('<link>').appendTo('head').attr({
        type: 'text/css',
        rel: 'stylesheet'
    }).attr('href', '../StaticFiles/css/font.css');
}

/****************************************
  ==== 3. SIDEBAR
****************************************/

$(window).on("load", function() {

    function loop(links, ul) {
        $.each(links, function(key, obj) {

            var $li = $('<li>');
            $li.appendTo(ul);

            var $page = location.href.split('/');
            $page = $page[$page.length - 1].split('.html')[0];
            var $targetpage = obj['link'].split('/');
            $targetpage = $targetpage[$targetpage.length - 1].split('.html')[0];

            var $class = ($page == $targetpage) ? 'current' : '';

            if (obj['link'] == 'null') {
                $li.text(obj['title']).addClass('nl').wrapInner('<span></span>');
            } 
            else {
                var $a = $('<a>').attr('href', obj['link'] + '.html').text(obj['title']);
                $($class != '') ? $a.attr('id', $class).attr('class', $class) : '';
                $a.appendTo($li);
            }

            if (obj['children'] && obj['children'].length > 0) {
                $li.prepend('<div class="arrow"></div>');
                var ul2 = $('<ul>').appendTo($li);
                loop(obj['children'], ul2);
            }
            else {
                $li.prepend('<div class="leafnode"></div>');
            }
        });
    }

    var data = toc;
    var ul = $('<ul/>');
    loop(data['children'], ul);
    $('.sidebar-menu .toc').append(ul);

    // Prepare sidebar list
    $('.sidebar-menu .toc').find('li:has(ul)').children('ul').hide();
    $('.sidebar-menu .toc').find('.arrow').addClass('collapsed');

    // If no current, find parent
    if ($('.sidebar-menu .toc .current').length == 0) {

        var url = location.href.split('/'),
            page = url[url.length - 1].split('.html')[0];

        // Find last occurrence of either '-' or '.' and split on this
        var splitAtPos = page.lastIndexOf('-');
        splitAtPos = Math.max(splitAtPos, page.lastIndexOf('.'));

        var parentPageName = page;
        if (splitAtPos != -1)
            parentPageName = page.substring(0, splitAtPos);

        var elems = $('.sidebar-menu .toc a[href="' + parentPageName + '.html"]');
        if (elems.length > 0) {
            elems.addClass('current');
        } 
        else {
            $('.sidebar-menu .toc a[href="' + location.href + '"]').addClass('current');
        }
    }

    // Auto expand current section in sidebar
    var parents = $('.sidebar-menu .toc .current').parents('ul');
    if (parents.length > 0) {
        parents.addClass('exp');
        $('ul.exp').removeAttr('style');
        $('.sidebar-menu ul.exp:first').removeAttr('class');
        $('ul.exp').parent().find('.arrow:first').removeClass('collapsed').addClass('expanded');
    }

    // Auto expand chidren of current section in sidebar
    var childList = $('.sidebar-menu .toc .current').parents('li').children('ul').first();
    childList.show();

    var arrow = $('.sidebar-menu .toc .current').parents('li').children('div').first();
    arrow.removeClass('collapsed');
    arrow.addClass('expanded');

    // Expand or collapse
    $('.sidebar-menu .toc li:has(ul) .arrow').click(function(e) {
        var arrow = $(this);
        if (arrow.hasClass('expanded')) {
            arrow.removeClass('expanded');
            arrow.addClass('collapsed');
        } 
        else {
            arrow.removeClass('collapsed');
            arrow.addClass('expanded');
        }
        arrow.parent().find('ul:first').toggle();
    });

    // For items with no link
    $('.sidebar-menu .toc .nl span').click(function(e) {
        var arrow = $(this).prev('.arrow');
        if (arrow.hasClass('expanded')) {
            arrow.removeClass('expanded');
            arrow.addClass('collapsed');
        } 
        else {
            arrow.removeClass('collapsed');
            arrow.addClass('expanded');
        }
        arrow.parent().find('ul:first').toggle();
    });

    // Scroll to the $('.sidebar-menu .toc .current')
    $('#customScrollbar').animate({
      scrollTop: $(".sidebar-menu .toc .current").offset().top-200
    }, 100);

});


$(document).ready(function() {

    /****************************************
      ==== 4. START-UP
    ****************************************/

    // Check left column hight
    columnHeight();

    // If offline, hide suggest button
    offline = true; //disable the suggest form for now
    if (offline) $('.suggest').addClass('hide').hide();

    // Hide empty prev/next bar on search page
    if ($('.search-results').length > 0) {
        $('.nextprev').hide();
    }

    // appends the previous page url to the form url to populate the form's url entry
    var feedbackURL = "https://docs.google.com/forms/d/19yizDXvahW1uelr_CHTnZnQjJ-Fid7Wo5OdUewhWPFs/viewform?embedded=true&entry.53114017=" + document.referrer;
    var feedbackIframeString = "<iframe src=" + feedbackURL + " width=100% height=\"1500\" frameborder=\"0\" marginheight=\"0\" marginwidth=\"0\">Loading...</iframe>";
    $("#feedback-container").html(feedbackIframeString);

    // Adds a copy button to code examples
    addCopyButtonsToCodeBlocks()
    
    /****************************************
      ==== 5. RESIZE EVENTS
    ****************************************/

    $(window).resize(function() {
        columnHeight();
    });


    /****************************************
      ==== 6. UI EVENTS
    ****************************************/

    // Toggle anything
    $('.toggle').on('click',function(e){
        var el = $(this).attr('data-target');
        ($(this).children(el).css('display') == 'block') ? $(this).children(el).hide() : $(this).children(el).show();
    });

    // Tool buttons tip
    $('.tt').hover(function() {
        var d = $(this).attr('data-distance').split('|');
        $('.tip', $(this)).css('top', d[0] + 'px');
        (d[2] == 'top') ? $('.tip', $(this)).addClass('t'): $('.tip', $(this)).addClass('b');
        $('.tip', $(this)).addClass('tip-visible');
        $(this).find('.tip').stop(true, true).removeClass('hide').animate({
            'top': d[1],
            'opacity': 1
        }, 200);
    }, function() {
        var d = $(this).attr('data-distance').split('|');
        $(this).find('.tip').stop(true, false).animate({
            'top': d[0],
            'opacity': 0
        }, 200, function() {
            $(this).addClass('hide').css('marginLeft', 0);
            $('.tip', $(this)).removeClass('tip-visible');
        });
    });

    if ("file:"==document.location.protocol){
        $('.lang-switcher').remove();
        $('.otherversionscontent').remove();
        $('.versionSwitcherArrow').remove();
        $('.version-number').css("cursor", "text");
        $('#otherVersionsContentMobileForm').remove();
    }

    // Hide elements on body click/touch
    $('html').on('click', function(e){
        if($(e.target).closest('.lang-switcher').length == 0){ $('.lang-list').hide(); }
        if($(e.target).closest('#VersionNumber').length == 0){ $('.otherversionscontent').hide(); }
    });
});


/****************************************
  ==== 7. GLOBAL FUNCTIONS
****************************************/

function columnHeight() {
    var tocHeight = getHeight() - 100;
    $('#sidebar .toc').css('height', tocHeight);
}

function getHeight(){
    if(typeof window.innerHeight != 'undefined'){
      var viewportheight = window.innerHeight;
    }
    else if(typeof document.documentElement != 'undefined' && typeof document.documentElement.clientHeight != 'undefined' && document.documentElement.clientHeight != 0){
      var viewportheight = document.documentElement.clientHeight;
    }
    else {
      var viewportheight = document.getElementsByTagName('body')[0].clientHeight;
    }
    return viewportheight;
  }
  function centerElement(parent,child,nested){
    if(nested){
      var el = parent.find(child);
      el.css('width', el.width());
      el.css('left', (parent.outerWidth(true) - el.outerWidth(true)/2 - parent.outerWidth(true)/2) - 3);
    }
  }

function GetQueryParameters() {
    var parameters = new Object();
    var l = window.location.search.substr(1).replace(/\+/g, ' ').split(/[&;]/);
    for (var i = 0; i < l.length; i++) {
        var p = l[i].split(/=/, 2);
        parameters[unescape(p[0])] = unescape(decodeURIComponent(p[1]));
    }
    return parameters;
}

function GetPageTitle(i) {
    return pages[info[i][1]][1];
}

function GetPageURL(i) {
    return pages[info[i][1]][0];
}

function GetPageSummary(i) {
    return info[i][0];
}

function GetSearchResults(terms, query) {
    var score = new Object();
    var min_score = terms.length;
    var found_common = new Array();
    for (var i = 0; i < terms.length; i++) {
        var term = terms[i];
        if (common[term]) {
            found_common.push(term);
            min_score--;
        }

        if (searchIndex[term]) {
            for (var j = 0; j < searchIndex[term].length; j++) {
                var page = searchIndex[term][j];
                if (!score[page])
                    score[page] = 0;
                ++score[page];
            }
        }

        for (var si in searchIndex) {
            if (si.slice(0, term.length) == term) {
                for (var j = 0; j < searchIndex[si].length; j++) {
                    var page = searchIndex[si][j];
                    if (!score[page])
                        score[page] = 0;
                    ++score[page];
                }
            }
        }
    }
    var results = new Array();
    for (var page in score) {

        var title = GetPageTitle(page);
        var summary = GetPageSummary(page);
        var url = GetPageURL(page);
        // ignore partial matches
        if (score[page] >= min_score) {
            results.push(page);

            var placement;
            // Adjust scores for better matches
            for (var i = 0; i < terms.length; i++) {
                var term = terms[i];
                if ((placement = title.toLowerCase().indexOf(term)) > -1) {
                    score[page] += 50;
                    if (placement == 0 || title[placement - 1] == '.')
                        score[page] += 500;
                    if (placement + term.length == title.length || title[placement + term.length] == '.')
                        score[page] += 500;
                } 
                else if ((placement = summary.toLowerCase().indexOf(term)) > -1)
                    score[page] += ((placement < 10) ? (20 - placement) : 10);
            }

            if (title.toLowerCase() == query)
                score[page] += 10000;
            else if ((placement = title.toLowerCase().indexOf(query)) > -1)
                score[page] += ((placement < 100) ? (200 - placement) : 100);
            else if ((placement = summary.toLowerCase().indexOf(query)) > -1)
                score[page] += ((placement < 25) ? (50 - placement) : 25);
        }
    }

    results = results.sort(function(a, b) {
        if (score[b] == score[a]) { // sort alphabetically by title if score is the same
            var x = GetPageTitle(a).toLowerCase();
            var y = GetPageTitle(b).toLowerCase();
            return ((x < y) ? -1 : ((x > y) ? 1 : 0));
        } 
        else { // else by score descending
            return score[b] - score[a]
        }
    });

    return results;
}

function PerformSearch() {
    var p = GetQueryParameters();
    var search = document.getElementById('q');
    if (search && p.q) {
        search.focus();
        search.value = p.q;
        search.select();
    }

    var query = p.q.replace(/^[\s.]+|[\s.]+$/g, '').toLowerCase();

    var terms;

    if (typeof(TinySegmenter) === "undefined") {
        terms = query.split(/[\s.]+/);
    } 
    else {
        var segmenter = new TinySegmenter();
        terms = segmenter.segment(query);
        terms = terms.filter(function(term) {
            return !/^\s+$/.test(term);
        });
    }

    var combined = query.replace(/\s+/g, '').toLowerCase();

    var results = GetSearchResults(terms, query);
    var results2 = GetSearchResults([combined], query);

    results = results.concat(results2);

    function unique(lst) {
        var a = lst.concat();
        for (var i = 0; i < a.length; ++i) {
            for (var j = i + 1; j < a.length; ++j) {
                if (a[i] === a[j])
                    a.splice(j--, 1);
            }
        }

        return a;
    };
    results = unique(results);

    if (results.length > 0) {
        if (p.redirect && (results.length == 1 || p.redirect <= score[results[0]] - score[results[1]])) {
            document.location = GetPageURL(results[0]) + ".html";
            return;
        }
        var html = '';
        html += '<h2>Your search for "<span class="q"></span>" resulted in ' + results.length + ' matches:</h2>';
        for (i in results) {
            var j = GetPageURL(results[i]);
            html += '<div class="result">';
            html += '<a href="' + GetPageURL(results[i]) + '.html" class="title">' + GetPageTitle(results[i]) + '</a>';
            if (p.show_score) html += '<i>Score: ' + score[results[i]] + '</i>';
            var summary = (GetPageSummary(results[i]).length > 130) ? GetPageSummary(results[i]).slice(0, 127) + '...' : GetPageSummary(results[i]);
            html += '<p>' + summary + '</p></div>';
        }
        $('.search-results').html(html);
        $("span.q")[0].innerText = p.q;
        $('.search-spinner').hide();
    } else {
        $('.search-results').html('Your search for "<b class="q"></b>" did not result in any matches. Please try again with a wider search');
        $("b.q")[0].innerText = p.q;
        $('.search-spinner').hide();
    }
}

////////////////////////////////////////////////////////////////////////////
// Add a Copy button to code examples
function addCopyButtonsToCodeBlocks() {
    // "pre > code" occurs in the manual, "pre.codeExampleCS" in the API (no code block)
    const codeBlocks = document.querySelectorAll('pre > code, pre.codeExampleCS');
    const copyIcon = "\ue963"; //Copy icon in UnityIcons.woff font
    const copyConfirmIcon = "\uea78"; //Copy confirmation icon in UnityIcons.woff font
    const copyButtonTitle = "Copy"; //Title text for copy button
    const copyConfirmMessage = "Copied"; //Text read after button is used

    codeBlocks.forEach(codeBlock => {
        let code = codeBlock.parentElement; // the parent PRE or DIV element

        // Ensure code block has a position for relative button placement
        if (getComputedStyle(code).position === 'static') {
            code.style.position = 'relative';
        }

        // Add code block container to tab order
        code.tabIndex = 0;

        // Create copy button
        const copyButton = document.createElement('button');
        copyButton.classList.add("copy-code");
        copyButton.innerHTML = copyIcon; 
        copyButton.title = copyButtonTitle;
        copyButton.setAttribute("type", "button");

        // Copy notification for screen reader
        const notification = document.createElement('div');
        notification.setAttribute("aria-live", "polite");
        notification.classList.add('sr-only'); //Bootstrap screen-reader only css class
        
        // Add elements directly to code block
        code.insertBefore(copyButton, code.firstChild); // Put at beginning because API code blocks have focasable links 
        code.appendChild(notification);

        /////// Event handling /////// 
        // Show/hide button on hover
        code.addEventListener('mouseenter', () => {
            copyButton.style.opacity = '1';
        });
        code.addEventListener('mouseleave', () => {
            copyButton.style.opacity = '0';
        });
        // Show/hide button on focus/blur of both pre block or button itself
        // to support keyboard tab navigation
        code.addEventListener('focus', () => {
            copyButton.style.opacity = '1';
        });
        code.addEventListener('blur', () => {
                copyButton.style.opacity = '0';
        });
        copyButton.addEventListener('focus', () => {
                copyButton.style.opacity = '1';
        });     
        copyButton.addEventListener('blur', () => {
                copyButton.style.opacity = '0';
        });

        // Copy inner text (removes html markup of syntax highlights and links)
        copyButton.addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(codeBlock.innerText);

                copyButton.innerHTML = copyConfirmIcon; //Check mark
                notification.textContent = copyConfirmMessage;
 
                setTimeout(() => {
                    copyButton.innerHTML = copyIcon; //Restore copy icon
                    notification.textContent = "";
                }, 1500);
            } catch (err) {
                console.error('Failed to copy text:', err);
            }
        });
        // Support keyboard activation of copy
        copyButton.addEventListener("keypress", function(event) {
            if (event.key === "Enter") {
                event.preventDefault();
                copyButton.click();
            }
        });
    });
}

/****************************************
  ==== 7. OTHER VERSIONS DISPLAY
****************************************/
var alreadyPopulated = false;
var populatedCounter = 0;
var numberOfVersions;
// Test offline code
// $(document).ready(function(){
//   // $.getJSON("../../../UnityVersionsInfo.json", function(data){
//   //       if (!alreadyPopulated){
//   //        populateOtherVersionsContainer(data);
//   //        alreadyPopulated = true;
//   //       }
//   // });
//   populateVersions();
// });

function populateVersions(){
  // $.getJSON("../../../StaticFilesConfig/UnityVersionsInfo.json", function(data){
  //   if (!alreadyPopulated){
  //     populateOtherVersionsContainer(data);
  //     alreadyPopulated = true;
  //   }

  // });
  if (!alreadyPopulated){
    populateOtherVersionsContainer(UnityVersionsInfo);
    alreadyPopulated = true;
  }
}

function setOtherVersionsDisplay(display_setting){
  if(display_setting){
      document.getElementById("OtherVersionsContent").style.display = "";
  }else{
      document.getElementById("OtherVersionsContent").style.display = "none";
  }

  if(display_setting === true){
    isUserOffline();
  }
}

function isUserOffline(){
  if(location.protocol == "file:"){
    document.getElementById("OtherVersionsContent").innerHTML = "Cannot access other versions offline!";
    return;
  }
}

function populateOtherVersionsContainer(versions){
  let currentVersionStringOrName = document.getElementsByClassName("version-number")[0].innerHTML.split("<b>")[1].split("</b>")[0];

  let otherVersionsContentUL = $('#OtherVersionsContentUl');

  // Create two divs, one for 'with this page', one without
  let versionsWithThisPageDiv = $('<div id="versionsWithThisPage"><li class="vsDivHeader"><p class="vsDivHeader"><b>Versions with this page:</b></p></li></div>').appendTo(otherVersionsContentUL);
  let versionsWithoutThisPageDiv = $('<div id="versionsWithoutThisPage"><li class="vsDivHeader"><p class="vsDivHeader"><b>Versions without this page:</b></p></li></div>').appendTo(otherVersionsContentUL);
  let versionsWithThisPageMobile = $('#versionsWithThisPageMobile');
  let versionsWithoutThisPageMobile = $('#versionsWithoutThisPageMobile');;

  numberOfVersions = versions.supported.length + versions.notSupported.length - 1;

  versions.supported.forEach(supportedVersion => {
    if(isVersionDifferent(currentVersionStringOrName, supportedVersion)){
      // Create a 'li' with nested 'a' tag then populate it with the link
      let linkText = supportedVersion.major + '.' + supportedVersion.minor;
      if (supportedVersion.hasOwnProperty("name")){
        linkText = supportedVersion.name;
      }
      let supportedVersionElement = $('<li class="LTS"><a id="' + supportedVersion.major + '.' + supportedVersion.minor + '" href="">' + linkText + '</a></li>').appendTo(versionsWithoutThisPageDiv);
      let supportedVersionElementMobile = $('<option id="' + supportedVersion.major + '-' + supportedVersion.minor + '" value="">' + linkText + ' - Supported</option>').appendTo(versionsWithoutThisPageMobile);
      supportedVersion.page = getCurrentOpenPage();
      processVersion(supportedVersion, onVersionSwitcherPopulated, {mobileVersionElement: supportedVersionElementMobile, versionElement: supportedVersionElement, versionsWithThisPageMobile: versionsWithThisPageMobile});
    }
  });

  versions.notSupported.forEach(notSupportedVersion => {
    if(isVersionDifferent(currentVersionStringOrName, notSupportedVersion)){
      let linkText = notSupportedVersion.major + '.' + notSupportedVersion.minor;
      if (notSupportedVersion.hasOwnProperty("name")){
        linkText = notSupportedVersion.name;
      }
      let notSupportedVersionElement = $('<li class="NLTS"><a id="' + notSupportedVersion.major + '.' + notSupportedVersion.minor + '" href="">' + linkText + '</a></li>').appendTo(versionsWithoutThisPageDiv);
      let notSupportedVersionElementMobile = $('<option id="' + notSupportedVersion.major + '-' + notSupportedVersion.minor + '" value="">' + linkText + ' - Not supported</option>').appendTo(versionsWithoutThisPageMobile);
      notSupportedVersion.page = getCurrentOpenPage();
      processVersion(notSupportedVersion, onVersionSwitcherPopulated, {mobileVersionElement: notSupportedVersionElementMobile, versionElement: notSupportedVersionElement, versionsWithThisPageMobile: versionsWithThisPageMobile});
    }
  });

  // Running before the Ajax calls return and populate the lists, hence removing the first list by default?

}

// Updates the other version list item when its page has been found
function updatePageFound(element, major, minor, elements){
  let parentElement = document.getElementById("versionsWithThisPage");
  let mobileVersionElement = elements.mobileVersionElement;

  // Append the element first if the pages found list only contains the header list item
  if (parentElement.childNodes.length <= 1){
    parentElement.appendChild(element);
    mobileVersionElement.appendTo(elements.versionsWithThisPageMobile);
    return;
  }

  // Always skip the header (start at 1)
  for (let i = 1; i < parentElement.childNodes.length; i++){
    let nodeID = parentElement.childNodes[i].childNodes[0].id.split('.');
    let nodeMajor = Number(nodeID[0]);
    let nodeMinor = Number(nodeID[1]);

    if (major > nodeMajor || (major == nodeMajor && minor >= nodeMinor)){
      mobileVersionElement.insertBefore(('#' + nodeMajor + '-' + nodeMinor));
      parentElement.insertBefore(element, parentElement.childNodes[i]);
      return;
    }
  }

  // Add element last if it doesn't fit elsewhere
  parentElement.appendChild(element);
  mobileVersionElement.appendTo(elements.versionsWithThisPageMobile);
}

function isVersionDifferent(currentVersionOrName, versionToCheck) {
  // currentVersionOrName will be one of "major-version.minor-version" for example "2023.2"
  // or "product name" for example "Unity 6 Preview".
  // Product Name was introduced during the 2023.3 to "Unity 6 Preview" rebrand in 2024.
  // Previous Unity versions do not have Product Names.
  let versionNumberStringToCheck = versionToCheck.major + '.' + versionToCheck.minor;

  if(currentVersionOrName == versionNumberStringToCheck || (
    versionToCheck.hasOwnProperty("name") && versionToCheck.name == currentVersionOrName))
    return false;

  return true;
}

function onVersionSwitcherPopulated(){
    if ($('#versionsWithThisPage').children().length <= 1){
      $('#versionsWithThisPage').hide();
      $('#versionsWithThisPageMobile').hide();
    }
    else if ($('#versionsWithoutThisPage').children().length <= 1){
      $('#versionsWithoutThisPage').hide();
      $('#versionsWithoutThisPageMobile').hide();
    }

    $('#versionsSelectMobile').change(function(e){
      if (e.target.value == "Select a different version"){
        return;
      }
      window.location.href = e.target.value;
    });
}

function processVersion(versionToCheck, finishedCallback, elements){
  var targetURL = getTargetUrlFromVersion(versionToCheck.major, versionToCheck.minor, versionToCheck.page);
  //attempt ajax request

  let versionName = versionToCheck.major + "." + versionToCheck.minor;
  let versionElement = document.getElementById(versionName);
  let mobileVersionElement = elements.mobileVersionElement;

  let fallbackURL = getTargetUrlFromVersion(versionToCheck.major, versionToCheck.minor, "ScriptReference/index.html");
  versionElement.href = fallbackURL; // Fallback...
  mobileVersionElement.val(fallbackURL);

  var xhttp = new XMLHttpRequest();
  xhttp.onreadystatechange = function() {
    if (this.readyState == 4) {
      this.responseText = "";
      if(this.status < 400 && this.status > 0){
        versionElement.href = this.responseURL;
        mobileVersionElement.val(this.responseURL);
        //mobileVersionElement.appendTo(elements.versionsWithThisPageMobile);

        //removeAsteriskFromElement(versionElement);
        updatePageFound(versionElement.parentNode, versionToCheck.major, versionToCheck.minor, elements);
        populatedCounter++;
        if (populatedCounter >= numberOfVersions){
          finishedCallback();
        }
      }
      else{
        // Link broken...
        populatedCounter++;
        if (populatedCounter >= numberOfVersions){
          finishedCallback();
        }
      }
    }
    else{
      // XMLHttpRequest broken...
    }
  }

  xhttp.open("HEAD", targetURL, true);
  xhttp.send();
}

function getCurrentOpenPage(){
  try {
    return window.location.href.split("/")[window.location.href.split("/").length - 2] + "/" + window.location.href.split("/")[window.location.href.split("/").length - 1];
  }
  catch(e){
    return window.location.href.split("/Documentation/")[1];
  }
}

function getTargetUrlFromVersion(major, minor, page){
  let pathSplit = location.pathname.split('/');
  let docsType = pathSplit[pathSplit.length - 2];
    if(major > 2000){
      return location.protocol + "//" + document.domain + "/" + major + "." + minor + "/Documentation/" + page;
    }else{
      return location.protocol + "//" + document.domain + "/" + major.toString() + minor.toString() + "0/Documentation/" + page;
    }
}


$(document).ready(function() {
    if (!("file:"==document.location.protocol))
    {
        $('a[href^="http://"]').attr("target", "_blank");
        $('a[href^="https://"]').attr("target", "_blank");
        populateVersions();
    }  
});

/****************************************
Glossary
****************************************/

$(document).ready(function() {

	window.onhashchange = function() {
		ShowGlossary();
	}

	ShowGlossary();


});

function ShowGlossary() {

	var area = $(location).attr('hash').slice(1);

    var isArea = false;
	$('.Glossary').each(function(i,item) {
        if (area == $(item)[0].title)
		{
            isArea = true;
		}
	});

    if (isArea)
    {
        $('.Glossary').each(function(i,item) {
            if (area != $(item)[0].title)
            {
                $(item)[0].style = 'display:none';
            } else {
                $(item)[0].style = '';
            }
        });
    } else {
        $('.Glossary').each(function(i,item) {
            $(item)[0].style = '';
        });
    }
}
