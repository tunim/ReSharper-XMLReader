using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICollection<IssueType.CategoryType> _categories;
        private readonly ICollection<string> _fileTypes;
        private ICollection<IssueType> _issueTypes;
        private ICollection<Issue> _issues;

        private readonly string _destinationPath;

        public HomeController()
        {
            _categories = new List<IssueType.CategoryType>();
            _fileTypes = new List<string>();
            _issueTypes = new List<IssueType>();
            _issues = new List<Issue>();

            _destinationPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", "resharper-results.xml");
        }

        public IActionResult Index(string[] categoryTypes = null, string[] severities = null, string[] issueTypes = null, string[] fileTypes = null)
        {
            if (ValidateXmlReport())
            {
                LoadXml();

                ViewBag.IssueTypes = new SelectList(_issueTypes, "Id", "Description");
                ViewBag.FileTypes = new SelectList(_fileTypes);
                ViewBag.CategoryTypes = new SelectList(_categories, "Id", "Description");
                ViewBag.Severities = new SelectList(_issueTypes.Select(x => x.Severity).Distinct().ToList());

                var issues = FilterIssues(categoryTypes, severities, issueTypes, fileTypes);

                return View(issues);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadAsync(IFormFile xml)
        {
            if (xml != null && xml.ContentType == "text/xml")
            {
                using (var stream = new FileStream(_destinationPath, FileMode.Create))
                    await xml.CopyToAsync(stream);

                if (ValidateXmlReport())
                    return RedirectToAction("Index");
                else
                {
                    System.IO.File.Delete(_destinationPath);
                    ModelState.AddModelError("xml", "The file loaded is not a valid ReSharper report.");
                }
            }
            else
                ModelState.AddModelError("xml", "Send a valid xml report");

            return View("Index");
        }

        private ICollection<Issue> FilterIssues(string[] categoryTypes, string[] severities, string[] issueTypes, string[] fileTypes)
        {
            var issues = _issues.AsEnumerable();

            if (categoryTypes != null && categoryTypes.Any())
                issues = issues.Where(x => categoryTypes.Contains(x.IssueType.Category.Id));

            if (severities != null && severities.Any())
                issues = issues.Where(x => severities.Contains(x.IssueType.Severity));

            if (issueTypes != null && issueTypes.Any())
                issues = issues.Where(x => issueTypes.Contains(x.IssueType.Id));

            if (fileTypes != null && fileTypes.Any())
                issues = issues.Where(x => fileTypes.Contains(x.FileType));

            return issues.ToList();
        }

        private bool ValidateXmlReport()
        {
            if (!System.IO.File.Exists(_destinationPath))
                return false;

            var xEle = XElement.Load(_destinationPath);
            var validXml = xEle.Element("IssueTypes") != null && xEle.Element("Issues") != null;

            var issueTypesValid = validXml && xEle.Element("IssueTypes").HasElements 
                && xEle.Element("IssueTypes").Elements().Any(x => x.HasAttributes
                    && x.Attribute("Id") != null
                    && x.Attribute("Description") != null
                    && x.Attribute("Severity") != null
                    && x.Attribute("CategoryId") != null
                    && x.Attribute("Category") != null);
            
            var issuesValid = validXml && xEle.Element("Issues").Elements().Any(x => x.HasAttributes
                && x.Attribute("Name") != null
                && x.Elements()
                    .Any(y => y.Name == "Issue"
                        && y.HasAttributes
                        && y.Attribute("TypeId") != null
                        && y.Attribute("File") != null
                        && y.Attribute("Message") != null));

            return issueTypesValid && issuesValid;
        }

        private void LoadXml()
        {
            var xEle = XElement.Load(_destinationPath);

            _issueTypes = LoadIssueTypes(xEle);
            _issues = LoadIssues(xEle);
            
        }

        private ICollection<IssueType> LoadIssueTypes(XElement xElement)
        {
            var issues = new List<IssueType>();
            foreach (var xEle in xElement.Element("IssueTypes").Elements())
            {
                var issueType = new IssueType
                {
                    Id = xEle.Attribute("Id").Value,
                    Description = xEle.Attribute("Description").Value,
                    Severity = xEle.Attribute("Severity").Value,
                    WikiUrl = xEle.Attribute("WikiUrl")?.Value,
                    Category = GetCategory(xEle)
                };

                issues.Add(issueType);
            }

            return issues;
        }
        
        private ICollection<Issue> LoadIssues(XElement xElement)
        {
            var issues = new List<Issue>();
            foreach (var xEleProject in xElement.Element("Issues").Elements())
            {
                foreach (var xEle in xEleProject.Elements())
                    issues.Add(GetIssue(xEleProject.Attribute("Name").Value, xEle));
            }

            return issues;
        }

        private Issue GetIssue(string projectName, XElement xElement)
        {
            var issue = new Issue
            {
                IssueTypeId = xElement.Attribute("TypeId").Value,
                File = xElement.Attribute("File").Value,
                Message = xElement.Attribute("Message").Value,
                ProjectName = projectName
            };

            issue.FileType = GetFileType(issue.File);
            issue.IssueType = GetIssueType(issue.IssueTypeId);

            if (xElement.Attribute("Line") != null)
                issue.LineNumber = int.Parse(xElement.Attribute("Line").Value);

            return issue;
        }

        private string GetFileType(string filePath)
        {
            var ext = $"*{filePath.Substring(filePath.LastIndexOf("."))}";

            if (!_fileTypes.Contains(ext))
                _fileTypes.Add(ext);
            return ext;
        }

        private IssueType GetIssueType(string issueTypeId)
        {
            return _issueTypes.FirstOrDefault(x => x.Id == issueTypeId);
        }

        private IssueType.CategoryType GetCategory(XElement xElement)
        {
            var categoryId = xElement.Attribute("CategoryId").Value;
            var categery = _categories.FirstOrDefault(x => x.Id == categoryId);
            if (categery == null)
            {
                categery = new IssueType.CategoryType
                {
                    Id = categoryId,
                    Description = xElement.Attribute("Category").Value
                };

                _categories.Add(categery);
            }
            
            return categery;
        }
    }
}
