﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using TeamBins.Common;

namespace TeamBins.DataAccess
{
    public interface IEmailRepository
    {
        Task<EmailTemplateDto> GetEmailTemplate(string name);
    }

    public class EmailRepository : BaseRepo, IEmailRepository
    {
        public EmailRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<EmailTemplateDto> GetEmailTemplate(string name)
        {
            var q = @"SELECT Name,EmailBody,EmailSubject as Subject FROM EmailTemplate  WITH (NOLOCK) WHERE Name=@name";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var teams = await con.QueryAsync<EmailTemplateDto>(q, new { @name = name });
                return teams.FirstOrDefault();
            }
        }
    }
    public interface ITeamRepository
    {
        TeamDto GetTeam(int teamId);
        TeamDto GetTeam(string name);
        int SaveTeam(TeamDto team);

        void SaveTeamMember(int teamId, int memberId, int createdById);
        void SaveDefaultProject(int userId, int teamId, int? selectedProject);
        List<TeamDto> GetTeams(int userId);

        void SaveDefaultTeamForUser(int userId, int teamId);
        MemberVM GetTeamMember(int teamId, int userId);
        void Delete(int id);
        Task<IEnumerable<TeamMemberDto>> GetTeamMembers(int teamId);

        Task<int> SaveTeamMemberRequest(AddTeamMemberRequestVM teamMemberRequest);
        Task<IEnumerable<AddTeamMemberRequestVM>> GetTeamMemberInvitations(int teamId);

        Task<AddTeamMemberRequestVM> GetTeamMemberInvitation(string activationCode);
        Task DeleteTeamMemberInvitation(int id);
    }

    public class TeamRepository : BaseRepo, ITeamRepository
    {
        public TeamRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public TeamDto GetTeam(int teamId)
        {
            var q = @"SELECT [Id],[Name],[IsPublic]  FROM Team WHERE ID=@id";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var teams = con.Query<TeamDto>(q, new { @id = teamId });
                return teams.FirstOrDefault();
            }
        }
        public TeamDto GetTeam(string name)
        {
            var q = @"SELECT [Id],[Name],[IsPublic]  FROM Team WHERE Name=@name";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var teams = con.Query<TeamDto>(q, new { @name = name });
                return teams.FirstOrDefault();
            }
        }
        public async Task<int> SaveTeamMemberRequest(AddTeamMemberRequestVM teamMemberRequest)
        {
            var q = @"INSERT INTO TeamMemberRequest(EmailAddress,TeamID,ActivationCode,CreatedByID,CreatedDate) VALUES(@email,@teamId,@a,@userId,@dt);;SELECT CAST(SCOPE_IDENTITY() as int)";
            using (var con = new SqlConnection(ConnectionString))
            {
                var a = Guid.NewGuid().ToString("n").Replace("-", "");
                con.Open();
                var p = await con.QueryAsync<int>(q, new
                {
                    @teamId = teamMemberRequest.TeamID,
                    @email = teamMemberRequest.EmailAddress,
                    @a = a,
                    @userId = teamMemberRequest.CreatedById,
                    @dt = DateTime.Now
                });
                return p.First();
            }
        }

        public async Task DeleteTeamMemberInvitation(int id)
        {
            var q = @"DELETE FROM TeamMemberRequest WHERE ID = @id";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                await con.ExecuteAsync(q, new { @id = id });
            }
        }

        public async Task<AddTeamMemberRequestVM> GetTeamMemberInvitation(string activationCode)
        {
            var q = @"SELECT 
                        TM.*,
                        U.ID,
                        U.FirstName as Name,
                        U.EmailAddress,
                        T.ID,
                        T.Name
                        FROM TeamMemberRequest TM
                        JOIN [User] U ON TM.CreatedByID = U.ID JOIN Team T ON T.ID=TM.TeamID WHERE ActivationCode=@code";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();

                var items =
                    await
                        con.QueryAsync<AddTeamMemberRequestVM, UserDto, TeamDto, AddTeamMemberRequestVM>(q,
                            (r, u, t) =>
                            {
                                r.CreatedBy = u;
                                r.Team = t;
                                return r;
                            },
                            new { @code = activationCode }, null, true, "ID,ID");

                return items.FirstOrDefault();


            }
        }
        public async Task<IEnumerable<AddTeamMemberRequestVM>> GetTeamMemberInvitations(int teamId)
        {
            var q = @"SELECT 
                        TM.*,
                        U.ID,
                        U.FirstName as Name,
                        U.EmailAddress,
                        T.ID,
                        T.Name
                        FROM TeamMemberRequest TM
                        JOIN [User] U ON TM.CreatedByID = U.ID JOIN Team T ON T.ID=TM.TeamID WHERE TeamID=@teamId";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();

                return
                    await
                        con.QueryAsync<AddTeamMemberRequestVM, UserDto, TeamDto, AddTeamMemberRequestVM>(q,
                            (r, u, t) =>
                            {
                                r.CreatedBy = u;
                                r.Team = t;
                                return r;
                            },
                            new { @teamid = teamId }, null, true, "ID,ID");




            }
        }

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembers(int teamId)
        {
            var q = @"SELECT U.ID,
                        U.FirstName,
                        U.EmailAddress,
                        U.LastLoginDate,
                        TM.CreatedDate as JoinedDate
                        FROM [dbo].[User] U WITH (NOLOCK) 
                        INNER JOIN TeamMember TM  WITH (NOLOCK) ON U.ID=TM.MemberID WHERE TM.TeamID=@teamId";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                return await con.QueryAsync<TeamMemberDto>(q, new { @teamId = teamId });
            }
        }
        public int SaveTeam(TeamDto team)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                if (team.Id == 0)
                {
                    var p = con.Query<int>("INSERT INTO Team(Name,CreatedDate,CreatedByID) VALUES (@name,@dt,@createdById);SELECT CAST(SCOPE_IDENTITY() as int)",
                                            new { @name = team.Name, @dt = DateTime.Now, @createdById = team.CreatedById });
                    team.Id = p.First();

                    con.Execute("INSERT INTO TeamMember(MemberID,TeamID,CreatedDate,CreatedByID) VALUES (@memberId,@teamId,@dt,@createdById)",
                                           new { memberId = team.CreatedById, @teamId = team.Id, @dt = DateTime.Now, @createdById = team.CreatedById });


                }
                else
                {
                    con.Query<int>("UPDATE Team SET Name=@name WHERE Id=@id", new { @name = team.Name, @id = team.Id });

                }
                return team.Id;

            }
        }

        public void SaveTeamMember(int teamId, int memberId, int createdById)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                con.Execute("INSERT INTO TeamMember(MemberID,TeamID,CreatedDate,CreatedByID) VALUES (@memberId,@teamId,@dt,@createdById)",
                                       new { memberId = memberId, @teamId = teamId, @dt = DateTime.Now, @createdById = memberId });

            }
        }

        public void SaveDefaultProject(int userId, int teamId, int? selectedProject)
        {
            throw new System.NotImplementedException();
        }

        public List<TeamDto> GetTeams(int userId)
        {
            var q =
              @" SELECT T.ID,T.Name,T.CreatedDate,T.CreatedByID,TeamMemberCount.Count as MemberCount 
                FROM Team T WITH (NOLOCK) 
                JOIN TeamMember TM  WITH (NOLOCK) ON T.ID=TM.TeamId
				JOIN (SELECT TeamId,COUNT(1) Count FROM TeamMember  WITH (NOLOCK)  group  by TeamId ) TeamMemberCount on TeamMemberCount.TeamId=T.ID
                WHERE @userId=@userId";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var projects = con.Query<TeamDto>(q, new { @userId = userId });
                return projects.ToList();
            }
        }

        public void SaveDefaultTeamForUser(int userId, int teamId)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(int id)
        {
            var q = @"DELETE FROM TeamMember WHERE TeamId=@teamId";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                con.Execute(q, new { @teamId = id });
            }
        }

        public MemberVM GetTeamMember(int teamId, int userId)
        {
            var q =
                @"SELECT [Id],[MemberID] ,[TeamID] ,[DefaultProjectID] FROM TeamMember  WITH (NOLOCK) WHERE TeamID=@t AND MemberID=@m";
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var projects = con.Query<MemberVM>(q, new { @t = teamId, @m = userId });
                return projects.FirstOrDefault();
            }
        }
    }
}