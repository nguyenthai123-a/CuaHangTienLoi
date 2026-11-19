using System.Collections.Generic;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface ICaService
    {
        int MoCa(int maQuay, int maThuNgan, decimal tienDauCa, int maTK, string ip, string userAgent);
        DongCaViewModel GetShiftCloseInfo(int maCa);
        void DongCa(DongCaViewModel model, int maNguoiChot, int maTK, string ip, string userAgent);
        BaoCaoCa? GetBaoCaoCa(int maCa);
        List<Ca> GetAllShifts();
    }
}
