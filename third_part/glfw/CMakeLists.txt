

add_library(glfw STATIC 
src/context.c
src/egl_context.c
src/egl_context.h
src/glfw3.pc.in
src/glfw3Config.cmake.in
src/glfw_config.h.in
src/init.c
src/input.c
src/internal.h
src/mappings.h
src/mappings.h.in
src/monitor.c
src/wgl_context.c
src/wgl_context.h
src/win32_init.c
src/win32_joystick.c
src/win32_joystick.h
src/win32_monitor.c
src/win32_platform.h
src/win32_thread.c
src/win32_time.c
src/win32_window.c
src/window.c
src/xkb_unicode.c
src/xkb_unicode.h
)

target_include_directories(glfw PUBLIC include)
target_link_libraries(glfw PRIVATE glad)
target_compile_definitions(glfw PUBLIC _GLFW_WIN32 _CRT_SECURE_NO_WARNINGS)

