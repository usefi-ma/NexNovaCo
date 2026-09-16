(function(){
  // --- helper to get id from URL ---
  function getQueryParam(name) {
    const params = new URLSearchParams(window.location.search);
    return params.get(name);
  }

  function setSocialLink(selector, url, accessibleName) {
    const link = document.querySelector(selector);
    if (!link) return;

    if (url && url !== "#") {
      link.href = url;
      link.setAttribute("aria-label", accessibleName);
      link.removeAttribute("aria-hidden");
    } else {
      link.removeAttribute("href");
      link.removeAttribute("aria-label");
      link.setAttribute("aria-hidden", "true");
    }
  }

  const memberId = getQueryParam("id");
  if(!memberId) {
    window.location.href = "team.html";
    return;
  }

  fetch("assets/data/member.json")
    .then(response => response.json())
    .then(data => {
      const member = data.find(m => m.id === memberId);
      if(!member) {
        window.location.href = "team.html";
        return;
      }

      // --- fill data ---
      document.title = `${member.name} - NexNovaCo`;
      document.querySelector(".breadcrumb-item.active").textContent = member.name;
      document.querySelector(".member_img img").src = member.image;
      document.querySelector(".member_img img").alt = member.name;
      document.querySelector("h2").textContent = member.name;
      document.querySelector("h4").textContent = member.role;
      document.querySelector(".member_desc p").textContent = member.bio;
      document.querySelector(".email_icon").href = `mailto:${member.email}`;
      setSocialLink(".linkedin_icon", member.linkedin, `${member.name} on LinkedIn`);
      setSocialLink(".telegram_icon", member.telegram, `${member.name} on Telegram`);

      // --- skills ---
      const skills = member.skills;
      const skillContainer = document.querySelectorAll(".skills .col-12.col-sm-6.col-md-6");
      if(skills.length >= 4){
        skillContainer[0].innerHTML = `
          <p>${skills[0]}</p>
          <p>${skills[1]}</p>
        `;
        skillContainer[1].innerHTML = `
          <p>${skills[2]}</p>
          <p>${skills[3]}</p>
        `;
      }
    })
    .catch(err => console.error("Error loading member:", err));
})();
