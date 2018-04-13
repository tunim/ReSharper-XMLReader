using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private ICollection<IssueType.CategoryType> _categories;
        private ICollection<string> _fileTypes;
        private ICollection<IssueType> _issueTypes;
        private ICollection<Issue> _issues;

        public HomeController()
        {
            _categories = new List<IssueType.CategoryType>();
            _fileTypes = new List<string>();
            _issueTypes = new List<IssueType>();
            _issues = new List<Issue>();

        }

        public IActionResult Index(string[] categoryTypes = null, string[] severities = null, string[] issueTypes = null, string[] fileTypes = null)
        {
            LoadXml();

            ViewBag.IssueTypes = new SelectList(_issueTypes, "Id", "Description");
            ViewBag.FileTypes = new SelectList(_fileTypes);
            ViewBag.CategoryTypes = new SelectList(_categories, "Id", "Description");
            ViewBag.Severities = new SelectList(_issueTypes.Select(x => x.Severity).Distinct().ToList());

            var issues = FilterIssues(categoryTypes, severities, issueTypes, fileTypes);

            return View(issues);
        }

        private void LoadXml()
        {
            var xmlPath = @"C:\ResharperResults\r-results.xml";
            var xEle = XElement.Load(xmlPath);

            _issueTypes = LoadIssueTypes(xEle);
            _issues = LoadIssues(xEle);
            
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

            if (!_fileTypes.Any(x => x == ext))
                _fileTypes.Add(ext);

            return ext;
        }

        private IssueType GetIssueType(string issueTypeId)
        {
            return _issueTypes.FirstOrDefault(x => x.Id == issueTypeId);
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
