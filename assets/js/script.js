$(document).ready(function () {
    if ($(".owl-carousel_project").length > 0) {
        var owl = $(".owl-carousel_project");
        owl.owlCarousel({ margin: 20, nav: !0, loop: true, dots: false, autoplay: true, autoplayTimeout: 3000, autoplayHoverPause: true,responsive: { 0: { items: 1 }, 600: { items: 2 }, 1000: { items: 3 } } });
    }
    if ($(".owl-carousel_partners").length > 0) {
        var owl = $(".owl-carousel_partners");
        owl.owlCarousel({ margin: 20, nav: !0, loop: true, autoplay: true, autoplayTimeout: 3000, autoplayHoverPause: true, responsive: { 0: { items: 1 }, 400: { items: 2 }, 700: { items: 3 }, 900: { items: 4 }, 1000: { items: 4 }, 1200: { items: 6 } } });
    }
    if ($(".owl-carousel_testimonial").length > 0) {
        var owl = $(".owl-carousel_testimonial");
        owl.owlCarousel({ items:1, nav: !0, loop: true, autoplay: true, autoplayTimeout: 6000, autoplayHoverPause: true });
    }
});
window.addEventListener("load", function () {
    if ($(".loading-container").length > 0 || $(".main_content").length > 0) {
        document.querySelector(".loading-container").style.display = "none";
        document.querySelector(".main_content").style.display = "block";
    }
});
AOS.init({ duration: 1000 });
$(function () {
    $(".gotop").click(function () {
        $("html , body").animate({ scrollTop: "0px" }, 1000);
    });
    $(window).scroll(function () {
        if ($(window).scrollTop() == 0) {
            $(".gotop").css("display", "none");
        } else {
            $(".gotop").css("display", "flex");
        }
        var scrollPosition = $(this).scrollTop();
        if (scrollPosition > 100) {
            $(".header_top").addClass("fixed");
        } else {
            $(".header_top").removeClass("fixed");
        }
    });
});

document.addEventListener("DOMContentLoaded", function () {
  const counterSection = document.querySelector(".counter");
  if (!counterSection) return; 
  if (typeof countUp === "undefined" || !countUp.CountUp) {
    console.warn("CountUp library not loaded. Skipping counter animation.");
    return;
  }

  const options = { duration: 2 };
  const countProjects = new countUp.CountUp("count-projects", 450, options);
  const countClients = new countUp.CountUp("count-clients", 3000, options);
  const countEmployees = new countUp.CountUp("count-employees", 1000, options);
  const countAwards = new countUp.CountUp("count-awards", 26, options);

  let hasAnimated = false;

  window.addEventListener("scroll", () => {
    const top = counterSection.getBoundingClientRect().top;
    if (top < window.innerHeight && !hasAnimated) {
      countProjects.start();
      countClients.start();
      countEmployees.start();
      countAwards.start();
      hasAnimated = true;
    }
  });
});
