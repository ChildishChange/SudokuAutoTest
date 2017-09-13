﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SudokuAutoTest
{
    class GitRepoHandler
    {
        //映射表,key 是学号,value 是Github项目地址
        private Dictionary<string, string> _gitMapTable;
        //作业映射表,key 是学号,value 是作业地址
        private Dictionary<string, string> _blogMapTable;
        //仓库映射表
        private Dictionary<string, string> _repoMapTable;
        //存放Git仓库的根目录
        private string _rootDir;
        //Github日志文件的名称
        private string _loggerFile;

        public GitRepoHandler(string rootDir)
        {
            _gitMapTable = new Dictionary<string, string>();
            _blogMapTable = new Dictionary<string, string>();
            _repoMapTable = new Dictionary<string, string>();
            _rootDir = rootDir;
            _loggerFile = Path.Combine(Program.LogDir, "git.log");
        }

        public void PreProcess(string htmlFile)
        {
            ExtractGithubRepo(htmlFile);
        }

        public void GetAllGithubRepos()
        {
            if (!File.Exists(Program.RepoFile))
            {
                LoadBlogMap(Program.BlogFile);
                LoadGithubMap(Program.GithubFile);
                GetRepoUrlFromBlogs();
                RecordRepoMapFile(false);
            }
            CloneRepos();
        }

        //Overview:获取学号为NumberId的Github仓库
        public void GetOneGithubRepo(string numberId)
        {
            if (!File.Exists(Program.RepoFile))
            {
                LoadBlogMap(Program.BlogFile);
                LoadGithubMap(Program.GithubFile);
                GetRepoUrlFromBlog(numberId);
                RecordRepoMapFile(true, numberId);
            }
            CloneRepo(numberId, true);
        }

        //Overview:从博客获取仓库的地址
        public void GetRepoUrlFromBlogs()
        {
            foreach (var key in _blogMapTable.Keys)
            {
                //Fetch content from blog
                GetRepoUrlFromBlog(key);
                Thread.Sleep(1000);
            }
        }

        //Overview: 从学生对应的博客中抽取
        public void GetRepoUrlFromBlog(string numberId)
        {
            string blogUrl = _blogMapTable[numberId];
            try
            {
                HttpClient client = new HttpClient();
                var uri = new Uri(blogUrl);
                HttpResponseMessage response = client.GetAsync(uri).Result;
                string blogContent = response.Content.ReadAsStringAsync().Result;
                //Match github pattern in html content
                Regex regex = new Regex($"{_gitMapTable[numberId]}.+?(?=(\"|\\s|<|/tree/))", RegexOptions.IgnoreCase);
                Match match = regex.Match(blogContent);
                if (match.Success)
                {
                    _repoMapTable[numberId] = match.Value;
                }
                Logger.Info($"Grab {numberId} blog , get github link :{_repoMapTable[numberId]}", _loggerFile);
            }
            catch (Exception)
            {
                Logger.Error($"Failed to fetch {numberId}'s blog, please check his blog again.", _loggerFile);
            }
        }

        public void RecordRepoMapFile(bool append, string numberId = "")
        {
            if (append && File.Exists(Program.RepoFile))
            {
                string[] lines = File.ReadAllLines(Program.RepoFile);
                foreach (var line in lines)
                {
                    string[] param = line.Split('\t');
                    if (!param[0].Equals(numberId))
                    {
                        _repoMapTable[param[0]] = param[1];
                    }
                }
            }
            using (var writer = new StreamWriter(Program.RepoFile))
            {
                foreach (var repo in _repoMapTable)
                {
                    writer.WriteLine(repo.Key + "\t" + repo.Value);
                }
            }
        }
        
        //Requires:RepoMapTable不为空
        //Effects:在_rootDir下Clone学生的项目仓库
        public void CloneRepos(bool rewrittern = false)
        {
            //Load file to repo map
            string[] lines = File.ReadAllLines(Program.RepoFile);
            foreach (var line in lines)
            {
                string[] param = line.Split('\t');
                _repoMapTable[param[0]] = param[1];
            }
            foreach(var key in _repoMapTable.Keys)
            {
                try
                {
                    CloneRepo(key, rewrittern);
                }
                catch (Exception e)
                {
                    Logger.Error($"Clone {key} Failed!\nMessage:{e.Message}", _loggerFile);
                }
            }
        }


        //Overview:克隆单个学生的项目
        public void CloneRepo(string numberId, bool rewrittern)
        {
            if (_repoMapTable.Keys.Contains(numberId))
            {
                string githubUrl = _repoMapTable[numberId];
                string clonePath = Path.Combine(_rootDir, numberId);
                //移动Repo到对应学号目录下
                if (!(Directory.Exists(clonePath) && !rewrittern))
                {
                    if (Directory.Exists(clonePath))
                    {
                        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        var timeStr = Convert.ToInt64(ts.TotalSeconds).ToString();
                        string newPath = Path.Combine(_rootDir, numberId + "-" + timeStr);
                        Directory.Move(clonePath, newPath);
                        Logger.Warning($"Project {clonePath} already exist. Move old one to {newPath}", _loggerFile);
                    }
                    Repository.Clone(githubUrl, clonePath);
                    Logger.Info($"Project {clonePath} successfully cloned!", _loggerFile);
                    Thread.Sleep(1000);
                }
            }
            else
            {
                Logger.Warning($"Student {numberId}'s blog doesn't have a github repo, please check his blog agian.", _loggerFile);
            }
        }

        //Overview:用于预处理的函数
        //Requires:从HTML文件中抽取学号对应的Github仓库地址
        //Effects:设置GitMapTable为给定值,生成映射表文件存储
        public void ExtractGithubRepo(string filePath)
        {

            //生成映射表,并对GitMapTable赋值
            Regex regex = new Regex(@">(?<GithubRep>.+github.com/.+)</a>\s+(?<NumID>\d{9})\s+");
            var matches = regex.Matches(File.ReadAllText(filePath));
            foreach (Match match in matches)
            {
                var numberId = match.Groups["NumID"].Value;
                var githubRepoUrl = match.Groups["GithubRep"].Value;
                _gitMapTable[numberId] = githubRepoUrl;
            }
            using (var sw = new StreamWriter("GitMappingConfig.txt"))
            {
                //生成对应的映射表
                foreach (var key in _gitMapTable.Keys)
                {
                    sw.WriteLine($"{key}\t{_gitMapTable[key]}");
                }
            }
        }

        //Overview:加载博客映射表, 默认在目录 BlogPath 下
        public void LoadBlogMap(string filePath)
        {
            var content = File.ReadAllLines(filePath);
            foreach (var line in content)
            {
                var keyAndValue = line.Split(new char[] { '\t' });
                _blogMapTable[keyAndValue[0]] = keyAndValue[1];
            }
        }

        //Overview:从文件加载学号与Github项目地址
        public void LoadGithubMap(string filePath)
        {
            var content = File.ReadAllLines(filePath);
            foreach (var line in content)
            {
                var keyAndValue = line.Split(new char[] { '\t' });
                _gitMapTable[keyAndValue[0]] = keyAndValue[1];
            }
        }
    }

}
