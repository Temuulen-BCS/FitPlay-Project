using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherProfilesController : ControllerBase
    {
        private readonly FitPlayContext _context;

        public TeacherProfilesController(FitPlayContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherProfile>>> GetAll()
        {
            return await _context.TeacherProfiles.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherProfile>> GetById(int id)
        {
            var teacher = await _context.TeacherProfiles.FindAsync(id);
            if (teacher == null) return NotFound();
            return teacher;
        }

        [HttpPost]
        public async Task<ActionResult<TeacherProfile>> Create(TeacherProfile profile)
        {
            _context.TeacherProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TeacherProfile input)
        {
            if (id != input.Id) return BadRequest();

            _context.Entry(input).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _context.TeacherProfiles.FindAsync(id);
            if (teacher == null) return NotFound();

            _context.TeacherProfiles.Remove(teacher);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
