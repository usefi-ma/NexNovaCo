$(document).ready(function () {
  $("#contactForm").on("submit", function (e) {
    e.preventDefault();

    // Get field values
    var firstName = $("#firstName").val().trim();
    var lastName = $("#lastName").val().trim();
    var email = $("#email").val().trim();
    var message = $("#message").val().trim();

    // Basic validation
    if (firstName === "" || lastName === "" || email === "" || message === "") {
      Swal.fire({
        icon: "error",
        title: "Please fill out all required fields.",
        confirmButtonColor: "#f97316"
      });
      return;
    }

    // Validate email format
    var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(email)) {
      Swal.fire({
        icon: "warning",
        title: "Invalid Email Address",
        text: "Please enter a valid email address.",
        confirmButtonColor: "#f97316"
      });
      return;
    }

    // Demo-only confirmation: no data is transmitted.
    Swal.fire({
      icon: "success",
      title: "Demo Submission",
      text: "Demo form submitted successfully. No message was sent.",
      confirmButtonColor: "#f97316"
    });

    // Reset form
    this.reset();
  });
});
