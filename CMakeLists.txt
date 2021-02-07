cmake_minimum_required(VERSION 3.12)
set(CMAKE_CXX_STANDARD 17)
include(cmake/ucm.cmake)

project(PBR)



add_subdirectory(third_part/glad)
add_subdirectory(third_part/glfw)
add_subdirectory(third_part/stb)





ucm_add_dirs(demo TO sources)

add_executable(Demo ${sources})
target_link_libraries(Demo PRIVATE glad glfw stb)

