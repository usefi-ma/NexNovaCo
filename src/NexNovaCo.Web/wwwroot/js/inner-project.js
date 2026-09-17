(function () {
  function getQueryParam(paramName) {
    var urlSearchParameters = new URLSearchParams(window.location.search);
    return urlSearchParameters.get(paramName);
  }

  var projectId = getQueryParam("id");
  if (!projectId) {
    window.location.href = "project.html";
    return;
  }

  // --- DOM hooks ---
  var projectTitleElement = document.getElementById("projectTitle");
  var projectMobileTitleElement = document.getElementById("projectMobileTitle");
  var projectSubtitleElement = document.getElementById("projectSubtitle");
  var projectSecondNameElement = document.getElementById("secondName");
  var projectSummaryElement = document.getElementById("projectSummary");
  var projectGalleryElement = document.getElementById("projectGallery");
  var projectDetailsListElement = document.getElementById("projectDetails");
  var projectFeaturesListElement = document.getElementById("projectFeatures");
  var breadcrumbCurrentElement = document.getElementById("breadcrumbCurrent");

  // --- fetch JSON ---
  fetch("assets/data/projects.json")
    .then(function(response){ return response.json(); })
    .then(function(projectsData){
      var currentProjectData = projectsData.find(function(p){ return p.id === projectId; });

      if (!currentProjectData) {
        window.location.href = "project.html";
        return;
      }

      // --- Fill header texts ---
      if (projectTitleElement) projectTitleElement.textContent = currentProjectData.name;
      if (projectMobileTitleElement) projectMobileTitleElement.textContent = currentProjectData.name;
      if (projectSubtitleElement) projectSubtitleElement.textContent = currentProjectData.subtitle;
      document.title = currentProjectData.name + " - NexNovaCo";

      // --- Main summary ---
      if (projectSummaryElement) projectSummaryElement.textContent = currentProjectData.summary;

      // --- Breadcrumb current ---
      if (breadcrumbCurrentElement) breadcrumbCurrentElement.textContent = currentProjectData.name;

      // --- Fill second name ---
      if (projectSecondNameElement) projectSecondNameElement.textContent = currentProjectData.secondname;

      if (projectGalleryElement && Array.isArray(currentProjectData.gallery)) {
        projectGalleryElement.innerHTML = ""; // clear placeholder

        currentProjectData.gallery.forEach(function(imageItem, index){
          var carouselItemElement = document.createElement("div");
          carouselItemElement.className = "carousel-item" + (index === 0 ? " active" : "");

          var imgElement = document.createElement("img");
          imgElement.src = imageItem.src;
          imgElement.alt = imageItem.alt || currentProjectData.name + " image";
          imgElement.loading = "lazy";

          carouselItemElement.appendChild(imgElement);
          projectGalleryElement.appendChild(carouselItemElement);
        });
      }

      // --- Details box ---
      if (projectDetailsListElement && currentProjectData.details) {
        var detailsHtml = "";
        if (currentProjectData.details.client) {
          detailsHtml += "<li><strong>Client:</strong> " + currentProjectData.details.client + "</li>";
        }
        if (currentProjectData.details.category) {
          detailsHtml += "<li><strong>Category:</strong> " + currentProjectData.details.category + "</li>";
        }
        if (currentProjectData.details.date) {
          detailsHtml += "<li><strong>Date:</strong> " + currentProjectData.details.date + "</li>";
        }
        if (currentProjectData.details.technologies) {
          detailsHtml += "<li><strong>Technologies:</strong> " + currentProjectData.details.technologies + "</li>";
        }
        projectDetailsListElement.innerHTML = detailsHtml;
      }

      // --- Features list ---
      if (projectFeaturesListElement && Array.isArray(currentProjectData.features)) {
        projectFeaturesListElement.innerHTML = currentProjectData
          .features
          .map(function(featureText, index){
            var aosDelay = 100 * (index + 1);
            return '<li data-aos="fade-up" data-aos-delay="'+aosDelay+'">'+featureText+'</li>';
          })
          .join("");
        }
      })
    .catch(function(error){
      console.error("Cannot load projects.json:", error);
      window.location.href = "project.html";
    });
})();
