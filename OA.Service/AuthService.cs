using Aspose.Pdf.Text;
using Aspose.Pdf;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OA.Core.Configurations;
using OA.Core.Constants;
using OA.Core.Models;
using OA.Core.Repositories;
using OA.Core.Services;
using OA.Core.Services.Helpers;
using OA.Domain.Services;
using OA.Domain.VModels;
using OA.Infrastructure.EF.Entities;
using OA.Repository;
using OA.Service.Helpers;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Claims;
using static OA.Core.Constants.CommonConstants;

namespace OA.Service
{
    public class AuthService : GlobalVariables, IAuthService
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly RoleManager<AspNetRole> _roleManager;
        private readonly IAspNetUserService _userService;
        private readonly IBaseRepository<Department> _departmentRepo;
        private readonly IJwtFactory _jwtFactory;
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly IMapper _mapper;
        private readonly IBaseRepository<SysFile> _sysFileRepo;

        public AuthService(UserManager<AspNetUser> userManager, RoleManager<AspNetRole> roleManager, IAspNetUserService userService,
            IJwtFactory jwtFactory, IOptions<JwtIssuerOptions> jwtOptions, IHttpContextAccessor contextAccessor, IBaseRepository<Department> departmentRepo,
            IBaseRepository<SysFile> sysFileRepo, IMapper mapper) : base(contextAccessor)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _departmentRepo = departmentRepo;
            _userService = userService;
            _jwtFactory = jwtFactory;
            _jwtOptions = jwtOptions.Value;
            _sysFileRepo = sysFileRepo;
            _mapper = mapper;
        }
        public async Task<AuthVModel> GenerateTokenJWT(ClaimsIdentity identity, string userName)
        {
            if (identity == null) return new AuthVModel();
            DateTime tokenIssuedTime = new DateTime();
            var jwt = new AuthVModel();
            if (DateTime.Now > tokenIssuedTime.Add(_jwtOptions.ValidFor))
            {
                jwt = await AuthTokens.GenerateJwt(identity, _jwtFactory, userName, _jwtOptions
                            , new JsonSerializerSettings { Formatting = Formatting.Indented });

                _jwtOptions.ValidFor = TimeSpan.FromMinutes(60);
            }
            return jwt;
        }

        List<MenuLeft>? CreateMenuLeft(string jsonUserHasFunctions)
        {
            if (jsonUserHasFunctions != null)
            {
                var menus = JsonConvert.DeserializeObject<List<MenuLeft>>(jsonUserHasFunctions);

                void AddChildMenus(MenuLeft parentMenu)
                {
                    var childMenus = menus.Where(menu => menu.ParentId == parentMenu.Id).ToList();

                    foreach (var childMenu in childMenus)
                    {
                        AddChildMenus(childMenu);

                        parentMenu.Childs.Add(childMenu);
                    }
                }

                if (menus == null)
                {
                    return null;
                }

                foreach (var menu in menus)
                {
                    if (menu.ParentId == null)
                    {
                        AddChildMenus(menu);
                    }
                }
                var menuLeft = menus.Where(menu => menu.ParentId == null).ToList();
                return menuLeft;
            }
            else
            {
                return null;
            }
        }

        public async Task<ResponseResult> Login(CredentialsVModel credentials)
        {
            var result = new ResponseResult();
            if (string.IsNullOrEmpty(credentials.Email) || string.IsNullOrEmpty(credentials.Password))
            {
                throw new BadRequestException(MsgConstants.Error404Messages.InvalidUsernameOrPassword);
            }

            var user = await _userManager.FindByEmailAsync(credentials.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(credentials.Email);
            }
            if (user != null && user.IsActive == CommonConstants.Status.Active)
            {
                if (await _userManager.CheckPasswordAsync(user, credentials.Password))
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var identity = await Task.FromResult(_jwtFactory.GenerateClaimsIdentity(credentials.Email, user.Id, roles.ToList()));
                    result.Data = await GenerateTokenJWT(identity, credentials.Email);
                }
                else
                {
                    throw new BadRequestException(MsgConstants.Error404Messages.InvalidPassword);
                }
            }
            else if (user != null && user.IsActive == CommonConstants.Status.InActive)
            {
                throw new BadRequestException(CommonConstants.AccountStatus.MsgInActive);
            }
            else
            {
                throw new BadRequestException(MsgConstants.Error404Messages.InvalidUsername);
            }
            return result;
        }



        public async Task<ExportStream> ExportPdf()
        {
            var result = await Me();

            if (result.Data == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin.");
            }

            var contract = result.Data;

            var contractDetails = new List<UserGetAllVModel>
            {
                new UserGetAllVModel
                {
                    AvatarPath=contract.AvatarPath,
                    EmployeeId=contract.EmployeeId,
                    FullName=contract.FullName,
                    DepartmentName=contract.DepartmentName,
                    Roles=contract.Roles,
                    UserName=contract.UserName,
                    Gender=contract.Gender,
                    Address=contract.Address,
                    Birthday=contract.Birthday,
                    Email=contract.Email,
                    PhoneNumber=contract.PhoneNumber,
                    StartDateWork=contract.StartDateWork,
                    Note=contract.Note
                }
            };

            // Thực hiện xuất PDF với dữ liệu hợp đồng
            var exportStream = ExportPdf("Employee", contractDetails);

            return exportStream;
        }

        private static List<string> GetHeaders(Type type)
        {
            var properties = type.GetProperties();
            var headers = new List<string>();
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(typeof(DataMemberAttribute), false);
                foreach (DataMemberAttribute dma in attributes.Cast<DataMemberAttribute>())
                {
                    if (!string.IsNullOrEmpty(dma.Name))
                    {
                        headers.Add(dma.Name);
                    }
                }
            }
            return headers;
        }

        public static ExportStream ExportPdf<T>(string fileName, IEnumerable<T> fileContent)
        {
            var objectType = typeof(T);
            var properties = objectType.GetProperties().ToList();

            var propertyTitles = new Dictionary<string, string>
            {
                { "EmployeeId", "Id nhân viên" },
                { "FullName", "Họ và tên nhân viên" },
                { "DepartmentName", "Tên phòng ban" },
                { "Roles", "Chức vụ" },
                { "UserName", "Tên tài khoản" },
                { "Gender", "Giới tính" },
                { "Address", "Địa chỉ" },
                { "Birthday", "Ngày sinh" },
                { "Email", "Email" },
                { "PhoneNumber", "Số điện thoại" },
                { "StartDateWork", "Ngày vào làm" },         
                { "Note", "Ghi chú" },
                
            };

            var document = new Document
            {
                PageInfo = new PageInfo
                {
                    Margin = new MarginInfo(28, 28, 28, 40)
                }
            };

            Page page = document.Pages.Add();

            foreach (var item in fileContent)
            {
                var avatarProperty = properties.FirstOrDefault(p => p.Name == "AvatarPath");
                var avatarPath =  avatarProperty != null ? "https://localhost:44381/" + avatarProperty.GetValue(item)?.ToString() 
                    : "https://localhost:44381/avatars/aa1678f0-75b0-48d2-ae98-50871178e9bd.jfif";

                if (!string.IsNullOrEmpty(avatarPath))
                {
                    MemoryStream imageMemoryStream = null;

                    if (avatarPath.StartsWith("http://") || avatarPath.StartsWith("https://"))
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var imageData = httpClient.GetByteArrayAsync(avatarPath).Result;
                            imageMemoryStream = new MemoryStream(imageData);
                        }
                    }
                    else
                    {
                        var localImageData = File.ReadAllBytes(avatarPath);
                        imageMemoryStream = new MemoryStream(localImageData);
                    }

                    if (imageMemoryStream != null)
                    {
                        var image = new Aspose.Pdf.Image
                        {
                            ImageStream = imageMemoryStream
                        };

                        image.FixWidth = 100;
                        image.FixHeight = 100;
                        page.Paragraphs.Add(image);
                    }
                }

                page.Paragraphs.Add(new TextFragment("\n"));

                foreach (var property in properties)
                {
                    var propertyName = property.Name;

                    if (propertyTitles.ContainsKey(propertyName))
                    {
                        var title = propertyTitles[propertyName];
                        var rowData = Convert.ToString(property.GetValue(item));

                        if (property.GetValue(item) != null && property.GetValue(item).GetType() == typeof(DateTime))
                        {
                            DateTime? date = (DateTime?)property.GetValue(item);
                            rowData = date?.ToString("dd-MM-yyyy");
                        }
                        else if (property.GetValue(item) != null && property.GetValue(item).GetType() == typeof(decimal))
                        {
                            decimal? number = (decimal?)property.GetValue(item);
                            NumberFormatInfo numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
                            numberFormatInfo.NumberDecimalSeparator = ",";
                            numberFormatInfo.NumberGroupSeparator = ".";
                            rowData = string.Format(numberFormatInfo, "{0:n}", number);
                        }
                        else if (property.GetValue(item) != null && (property.GetValue(item).GetType() == typeof(bool) || property.GetValue(item).GetType() == typeof(bool?)))
                        {
                            bool? boolValue = property.GetValue(item) as bool?;
                            rowData = boolValue.HasValue ? (boolValue.Value ? "Nam" : "Nữ") : "Khác";
                        }
                        else if (property.GetValue(item) is IEnumerable<object> enumerableValue)
                        {
                            rowData = string.Join(", ", enumerableValue);
                        }

                        var propertyText = $"{title}: {rowData}";
                        var propertyFragment = new TextFragment(propertyText)
                        {
                            TextState =
                            {
                                Font = FontRepository.FindFont("Arial"),
                                FontSize = 15,
                                ForegroundColor = Color.Black
                            }
                        };

                        page.Paragraphs.Add(propertyFragment);
                        page.Paragraphs.Add(new TextFragment("\n"));
                    }
                }


            }

            var outputStream = new MemoryStream();
            document.Save(outputStream);
            outputStream.Position = 0;

            return new ExportStream
            {
                FileName = $"{fileName}{CommonConstants.Pdf.fileNameExtention}",
                Stream = outputStream,
                ContentType = Pdf.format
            };
        }


        public async Task<ResponseResult> Me()
        {
            var result = new ResponseResult();

            var entity = await _userManager.FindByIdAsync(GlobalUserId ?? string.Empty);
            if (entity == null)
            {
                throw new BadRequestException(MsgConstants.WarningMessages.NotFoundData);
            }

            var entityRoles = await _userManager.GetRolesAsync(entity);
            var model = _mapper.Map<AspNetUser, GetMeVModel>(entity);

            if (entity.AvatarFileId != null)
            {
                var entityFile = await _sysFileRepo.GetById((int)entity.AvatarFileId);
                if (entityFile != null)
                {
                    model.AvatarPath = entityFile.Path;
                }
            }

            if (entityRoles != null)
            {
                model.Roles = (List<string>)entityRoles;
            }

            var dept = await _departmentRepo.GetById(model.DepartmentId);

            model.DepartmentName = dept?.Name ?? string.Empty;

            var roleJsons = new List<string>();

            foreach (var roleName in model.Roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName) ?? new AspNetRole();
                if (role.IsAdmin)
                {
                    model.IsAdmin = true;
                }
                if (role.JsonRoleHasFunctions != null)
                {
                    roleJsons.Add(role.JsonRoleHasFunctions.ToString());
                }
            }

            var allRoles = roleJsons
                .Where(json => !string.IsNullOrWhiteSpace(json))
                .SelectMany(json =>
                {
                    return JsonConvert.DeserializeObject<List<MenuLeft>>(json) ?? new List<MenuLeft>();
                })
                .ToList();

            var mergedRoles = allRoles
            .GroupBy(role => role.Id)
            .Select(group =>
            {
                var merged = group.First();
                merged.Function = new Function
                {
                    IsAllowAll = group.Any(r => r.Function.IsAllowAll),
                    IsAllowView = group.Any(r => r.Function.IsAllowView),
                    IsAllowCreate = group.Any(r => r.Function.IsAllowCreate),
                    IsAllowEdit = group.Any(r => r.Function.IsAllowEdit),
                    IsAllowPrint = group.Any(r => r.Function.IsAllowPrint),
                    IsAllowDelete = group.Any(r => r.Function.IsAllowDelete)
                };
                return merged;
            })
            .ToList();

            model.MenuLeft = mergedRoles;

            result.Data = model;
            return result;
        }
    }
}
