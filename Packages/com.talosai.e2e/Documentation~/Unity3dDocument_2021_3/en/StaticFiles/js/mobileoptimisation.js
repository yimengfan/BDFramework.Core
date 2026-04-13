const mobileWindowWidth = 900;

$(function () {
  initializeSidebar();

  $('html').on('click', function(e){
    if($(window).width() < mobileWindowWidth && $(e.target).closest('#sidebar').length == 0 && $(e.target).closest('#nav-open').length == 0){ $('.sidebar').trigger("sidebar:close"); }
  });
});  
/****************************************
 Sidebar
  ****************************************/

function initializeSidebar() {

  $('#content-wrap').addClass('opened-sidebar');

  $('html').on('click', function (e) {
    if ($(e.target).closest('.sidebar').length == 0) {
      if ($(window).width() < mobileWindowWidth && $(".opened-sidebar").length !== 0) {
        $(".sidebar").trigger("sidebar:close");
        checkSidebarClasses(false);
      }
    }

  });

  var sidebar = $(".sidebar");
  sidebar.sidebar();

  //$('.darkCover').hide();
  $('.darkCover').addClass('hideBackground');
  
  sidebar.removeClass("hidden");

  $(window).on("resize", function () {
    if ($(window).width() > mobileWindowWidth && $(".opened-sidebar").length === 0) {
      sidebar.trigger("sidebar:open", [{speed: 0}]);
    }

    checkSidebarClasses();
  });
  $("#nav-open").click(function () {
    sidebar.trigger("sidebar:toggle");
  });

  sidebar.on("sidebar:opened", function () {
    $('#nav-open').addClass("active");
    $("#content-wrap").addClass("opened-sidebar");
    checkSidebarClasses(true);
  });

  sidebar.on("sidebar:closed", function () {
    $('#nav-open').removeClass("active");
    $("#content-wrap").removeClass("opened-sidebar");
    checkSidebarClasses(false);
  });

  if ($(window).width() < mobileWindowWidth) {
    sidebar.trigger("sidebar:close", [{speed: 0}]);
    checkSidebarClasses(false);
    
  } else {
    sidebar.trigger("sidebar:open", [{speed: 0}]);
  }
}

function checkSidebarClasses(open){
  if ($(window).width() < mobileWindowWidth && open){
    $('.darkCover').removeClass('hideBackground');
    $('.sidebar-wrap').addClass("shadowEffect");
    //$('.darkCover').show();
  }
  else if ($(window).width() >= mobileWindowWidth && open){
    $('.sidebar-wrap').removeClass("shadowEffect");
    //$('.darkCover').hide();
    $('.darkCover').addClass('hideBackground');
  }
  else  {
    $('.sidebar-wrap').removeClass("shadowEffect");
    //$('.darkCover').hide();
    $('.darkCover').addClass('hideBackground');
  }
}