using ApiMarafons.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Routing;
using System.Xml.Linq;

namespace ApiMarafons.App_Start
{
    [RoutePrefix("api/data")]
    public class DataController : ApiController
    {
        private readonly MARAFON_DBEntities _context;

        public DataController()
        {
            _context = new MARAFON_DBEntities();
        }

        [HttpPost]
        [Route("getdata")]
        public async Task<IHttpActionResult> GetData([FromBody] LoginRequest request)
        {
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.login == request.Login && u.password == request.Password);

            if (user == null)
            {
                return NotFound();
            }

            if (user.type == 2)
            {
                var zriteliData = await _context.ZRITELI
                    .Where(z => z.UID == user.UID)
                    .Select(z => new
                    {
                        z.UID,
                        z.fam,
                        z.name,
                        z.otch,
                        z.e_mail,
                        z.number_phone,
                        UserType = user.type // Добавляем поле type
                    })
                    .FirstOrDefaultAsync();

                if (zriteliData == null)
                {
                    return NotFound();
                }
                return Ok(zriteliData);
            }
            else if (user.type == 1)
            {
                var sportsmeniData = await _context.SPORTMENS
                    .Where(s => s.UID == user.UID)
                    .Select(s => new
                    {
                        s.UID,
                        s.fam,
                        s.name,
                        s.otch,
                        s.pol,
                        s.bday,
                        s.strana,
                        UserType = user.type // Добавляем поле type
                    })
                    .FirstOrDefaultAsync();

                if (sportsmeniData == null)
                {
                    return NotFound();
                }
                return Ok(sportsmeniData);
            }
            else
            {
                return BadRequest("Invalid user type");
            }
        }
        
        [HttpPost]
        [Route("EstChel")]
        public async Task<IHttpActionResult> GetLogin([FromBody] LoginRequest request)
        {
            var user = await _context.users
                .AnyAsync(u => u.login == request.Login );
           if(!user)
            {
                return NotFound();
            }
           return Ok(user);
        }


        [HttpPost]
        [Route("register")]
        public async Task<IHttpActionResult> Register([FromBody] RegisterRequest request)
        {
            if (_context.users.Any(u => u.login == request.Login))
            {
                return BadRequest("Login already exists.");
            }



       
            await _context.SaveChangesAsync();
            var UIDYu = 1; var UIDYa = 1;
            // Добавление данных в соответствующую таблицу в зависимости от типа пользователя
            switch (request.Type)
            {
                case 1: // Спортсмен
                    var athlete = new SPORTMENS
                    {
                        fam = request.Fam,
                        name = request.Name,
                        otch = request.Otch,
                        bday = request.Birthdate,
                        pol = request.Gender,
                        strana = request.Country
                    };
                    _context.SPORTMENS.Add(athlete);
                    await _context.SaveChangesAsync();
                    UIDYa = athlete.UID;
                    break;
                case 2: // Зритель
                    var spectator = new ZRITELI
                    {
                        fam = request.Fam,
                        name = request.Name,
                        otch = request.Otch,
                        e_mail = request.Email,
                        number_phone = request.Phone
                    };
                    _context.ZRITELI.Add(spectator);
                    await _context.SaveChangesAsync();
                    UIDYu = spectator.UID;
                    break;
                    // Добавьте другие типы пользователей, если необходимо
            }
            
            var user = new users
            {
                UID = (request.Type == 1) ? UIDYa : UIDYu ,
                login = request.Login,
                password = request.Password,
                type = request.Type
            };
            _context.users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new { user.ID, user.type });
        }
    }
    [RoutePrefix("api/marathons")]
    public class MarathonsController : ApiController
    {
        private readonly MARAFON_DBEntities _context;

        public MarathonsController()
        {
            _context = new MARAFON_DBEntities();
        }

        [HttpGet]
        
        [Route("")]
        public async Task<IHttpActionResult> GetMarathons()
        {
            var marathons = await _context.MARAFON.ToListAsync();
            return Ok(marathons);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> GetMarathon(int id)
        {
            var marathonsUchasniki = await _context.MARAFON_UCHASTIE.Where(u => u.MUID == id).ToListAsync();
            var allUchastniki = await _context.SPORTMENS.ToListAsync();
            var result = new List<object>();
            List<int?> SUIDmm = new List<int?>();
            if (marathonsUchasniki == null)
            {
                return NotFound();
            }
            foreach (MARAFON_UCHASTIE uCHASTIE in marathonsUchasniki)
            {
                SUIDmm.Add(uCHASTIE.SUID);
            }
            foreach(SPORTMENS sPORTMENS in allUchastniki)
            {
                if(SUIDmm.Contains(sPORTMENS.UID))
                {
                    result.Add(new
                    {
                        fam = sPORTMENS.fam,
                        name = sPORTMENS.name,
                        otch = sPORTMENS.otch,
                        pol = sPORTMENS.pol,
                        bday = sPORTMENS.bday,
                        strana = sPORTMENS.strana
                    });
                    //public string fam { get; set; }
                    //public string name { get; set; }
                    //public string otch { get; set; }
                    //public Nullable<bool> pol { get; set; }
                    //public Nullable<System.DateTime> bday { get; set; }
                    //public string strana { get; set; }
                }
            }
            return Ok(result);


        }
    }
    [RoutePrefix("api/athletes")]
    public class AthletesController : ApiController
    {
        private readonly MARAFON_DBEntities _context;

        public AthletesController()
        {
            _context = new MARAFON_DBEntities();
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> GetAthlete(int id)
        {
            List<int?> MUIDmm = new List<int?>();
            var marathonsUchasniki = await _context.MARAFON_UCHASTIE.Where(u => u.SUID == id).ToListAsync();
            var result = new object();
            foreach (MARAFON_UCHASTIE mARAFON_UCHASTIE in marathonsUchasniki)
            {
                MUIDmm.Add(mARAFON_UCHASTIE.MUID);
            }
            result = (MUIDmm);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        [Route("registerM")]
        public async Task<IHttpActionResult> RegisterForMarathon([FromBody] MarathonRegistrationRequest request)
        {
            if (_context.MARAFON_UCHASTIE.Any(m => m.MUID == request.MarathonId && m.SUID == request.AthleteId))
            {
                return BadRequest("Athlete is already registered for this marathon.");
            }

            var registration = new MARAFON_UCHASTIE
            {
                MUID = request.MarathonId,
                SUID = request.AthleteId
            };

            _context.MARAFON_UCHASTIE.Add(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Route("unregisterM")]
        public async Task<IHttpActionResult> UnregisterFromMarathon([FromBody] MarathonRegistrationRequest request)
        {
            var registration = await _context.MARAFON_UCHASTIE
                .FirstOrDefaultAsync(m => m.MUID == request.MarathonId && m.SUID == request.AthleteId);

            if (registration == null)
            {
                return NotFound();
            }

            _context.MARAFON_UCHASTIE.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
    [RoutePrefix("api/spectators")]
    public class SpectatorsController : ApiController
    {
        private readonly MARAFON_DBEntities _context;

        public SpectatorsController()
        {
            _context = new MARAFON_DBEntities();
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> GetSpectator(int id)
        {
            var spectator = await _context.ZRITELI.FindAsync(id);
            if (spectator == null)
            {
                return NotFound();
            }
            return Ok(spectator);
        }

        [HttpGet]
        [Route("marathons")]
        public async Task<IHttpActionResult> GetMarathonsWithAthletes()
        {
            var marathons = await _context.MARAFON.ToListAsync();

            var result = marathons.Select(m => new
            {
                m.UID,
                m.NAME,
                m.DLINA,
                m.DATE_START,
                m.VZNOS,
                Athletes = _context.MARAFON_UCHASTIE
                .Where(mu => mu.MUID == m.UID)  // Замените MarafonId на имя столбца с ID марафона в Marafon_uchastie
                .Join(_context.SPORTMENS,
                    mu => mu.SUID,             // Замените AthleteId на имя столбца с ID атлета в Marafon_uchastie
                    u => u.UID,                     // UID - ID атлета в UChastniki
                    (mu, u) => new
                    {
                        u.fam,
                        u.name,
                        u.otch,
                        u.pol,
                        u.bday,
                        u.strana
                    })
                .ToList()

            });

            return Ok(result);
        }
    }


    public class MarathonRegistrationRequest
    {
        public int MarathonId { get; set; }
        public int AthleteId { get; set; }
    }


    public class LoginRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
    public class RegisterRequest
    {
        public int UID { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int Type { get; set; }
        public string Fam { get; set; } // Фамилия
        public string Name { get; set; } // Имя
        public string Otch { get; set; } // Отчество
        public DateTime? Birthdate { get; set; } // День рождения (для спортсменов)
        public bool Gender { get; set; } // Пол (для спортсменов)
        public string Country { get; set; } // Страна (для спортсменов)
        public string Email { get; set; } // Email (для зрителей)
        public string Phone { get; set; } // Номер телефона (для зрителей)
    }

}
